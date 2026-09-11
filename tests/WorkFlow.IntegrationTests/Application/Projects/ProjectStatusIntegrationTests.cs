using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.PauseProject;
using WorkFlow.Application.Projects.ResumeProject;
using WorkFlow.Application.Projects.StartProject;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.Projects;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ProjectStatusIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ProjectStatusIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
    StartProjectHandler_ShouldPersistInProgress_WhenRequesterIsTenantAdmin()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                grantEditProjectPermission: false,
                ProjectStatus.Planning);

        var handler =
            CreateStartProjectHandler(
                context);

        var result =
            await handler.HandleAsync(
                new StartProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            ProjectStatus.InProgress,
            result.Value!.Status);

        context.ChangeTracker.Clear();

        var persistedProject =
            await context.Projects
                .AsNoTracking()
                .SingleAsync(
                    project =>
                        project.PublicId ==
                        scenario.Project.PublicId);

        Assert.Equal(
            ProjectStatus.InProgress,
            persistedProject.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    PauseProjectHandler_ShouldPersistPaused_WhenMemberHasEditProjectPermission()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member,
                grantEditProjectPermission: true,
                ProjectStatus.InProgress);

        var handler =
            CreatePauseProjectHandler(
                context);

        var result =
            await handler.HandleAsync(
                new PauseProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId,
                    "Aguardando retorno externo."));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            ProjectStatus.Paused,
            result.Value!.Status);

        context.ChangeTracker.Clear();

        var persistedProject =
            await context.Projects
                .AsNoTracking()
                .SingleAsync(
                    project =>
                        project.PublicId ==
                        scenario.Project.PublicId);

        Assert.Equal(
            ProjectStatus.Paused,
            persistedProject.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    ResumeProjectHandler_ShouldPersistInProgressAndNewDueDate_WhenMemberHasEditProjectPermission()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member,
                grantEditProjectPermission: true,
                ProjectStatus.Paused);

        var newDueDate =
            new DateTime(
                2027,
                2,
                28,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var handler =
            CreateResumeProjectHandler(
                context);

        var result =
            await handler.HandleAsync(
                new ResumeProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId,
                    newDueDate));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            ProjectStatus.InProgress,
            result.Value!.Status);

        Assert.Equal(
            newDueDate,
            result.Value.DueDate);

        context.ChangeTracker.Clear();

        var persistedProject =
            await context.Projects
                .AsNoTracking()
                .SingleAsync(
                    project =>
                        project.PublicId ==
                        scenario.Project.PublicId);

        Assert.Equal(
            ProjectStatus.InProgress,
            persistedProject.Status);

        Assert.Equal(
            newDueDate,
            persistedProject.DueDate);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    StartProjectHandler_ShouldNotChangeProject_WhenMemberDoesNotHaveEditProjectPermission()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member,
                grantEditProjectPermission: false,
                ProjectStatus.Planning);

        var handler =
            CreateStartProjectHandler(
                context);

        var result =
            await handler.HandleAsync(
                new StartProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.StatusChangeNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedProject =
            await context.Projects
                .AsNoTracking()
                .SingleAsync(
                    project =>
                        project.PublicId ==
                        scenario.Project.PublicId);

        Assert.Equal(
            ProjectStatus.Planning,
            persistedProject.Status);

        await transaction.RollbackAsync();
    }

    private static StartProjectHandler
        CreateStartProjectHandler(
            WorkFlowDbContext context)
    {
        return new StartProjectHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            context);
    }

    private static PauseProjectHandler
        CreatePauseProjectHandler(
            WorkFlowDbContext context)
    {
        return new PauseProjectHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            context);
    }

    private static ResumeProjectHandler
        CreateResumeProjectHandler(
            WorkFlowDbContext context)
    {
        return new ResumeProjectHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            context);
    }

    private static async Task<Scenario>
        CreateScenarioAsync(
            WorkFlowDbContext context,
            UserRole requesterRole,
            bool grantEditProjectPermission,
            ProjectStatus initialStatus)
    {
        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Status {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var tenantAdmin =
            new User(
                tenant.Id,
                "Administrador",
                $"admin-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        User requester;

        if (requesterRole == UserRole.TenantAdmin)
        {
            requester =
                tenantAdmin;

            context.Users.Add(
                tenantAdmin);
        }
        else
        {
            requester =
                new User(
                    tenant.Id,
                    "Usuário Solicitante",
                    $"requester-{uniqueValue}@test.local",
                    "password-hash",
                    requesterRole);

            context.Users.AddRange(
                tenantAdmin,
                requester);
        }

        await context.SaveChangesAsync();

        var dueDate =
            new DateTime(
                2026,
                12,
                31,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var project =
            new Project(
                tenant.Id,
                $"Projeto Status {uniqueValue}",
                tenantAdmin.Id,
                "Projeto utilizado no teste de integração.",
                dueDate: dueDate);

        switch (initialStatus)
        {
            case ProjectStatus.Planning:
                break;

            case ProjectStatus.InProgress:
                project.Start();
                break;

            case ProjectStatus.Paused:
                project.Start();

                project.Pause(
                    "Pausa inicial do cenário.");
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(initialStatus),
                    initialStatus,
                    "Status inicial não suportado pelo cenário.");
        }

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        if (requesterRole != UserRole.TenantAdmin)
        {
            var projectMember =
                new ProjectMember(
                    project.Id,
                    requester.Id,
                    tenantAdmin.Id);

            context.ProjectMembers.Add(
                projectMember);

            await context.SaveChangesAsync();

            if (grantEditProjectPermission)
            {
                var permission =
                    new ProjectMemberPermission(
                        projectMember.Id,
                        ProjectPermission.EditProject,
                        tenantAdmin.Id);

                context.ProjectMemberPermissions.Add(
                    permission);

                await context.SaveChangesAsync();
            }
        }

        context.ChangeTracker.Clear();

        return new Scenario(
            tenant,
            requester,
            project);
    }

    private sealed record Scenario(
        Tenant Tenant,
        User Requester,
        Project Project);
}