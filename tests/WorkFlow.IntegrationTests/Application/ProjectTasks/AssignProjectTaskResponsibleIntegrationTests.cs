using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.AssignProjectTaskResponsible;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class AssignProjectTaskResponsibleIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    public AssignProjectTaskResponsibleIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.Planning)]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.InProgress)]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.Paused)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.Planning)]
    [InlineData(UserRole.Member, ProjectStatus.Paused)]
    public async Task
        HandleAsync_ShouldPersistResponsible_WhenAuthorized(
            UserRole requesterRole,
            ProjectStatus projectStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                requesterRole,
                projectStatus);

        context.ChangeTracker.Clear();

        var before =
            DateTime.UtcNow;

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.True(
            result.IsSuccess);

        Assert.Null(
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(
                    task =>
                        task.PublicId ==
                        scenario.Task.PublicId);

        Assert.Equal(
            scenario.ResponsibleUser.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            persistedTask.Status);

        Assert.NotNull(
            persistedTask.UpdatedAt);

        Assert.InRange(
            persistedTask.UpdatedAt!.Value,
            before,
            DateTime.UtcNow);

        Assert.Equal(
            scenario.Task.PublicId,
            result.Value!.PublicId);

        Assert.Equal(
            scenario.Tenant.PublicId,
            result.Value.TenantPublicId);

        Assert.Equal(
            scenario.Project.PublicId,
            result.Value.ProjectPublicId);

        Assert.Equal(
            scenario.ResponsibleUser.PublicId,
            result.Value.ResponsibleUserPublicId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            result.Value.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReassignResponsible_WithoutChangingInProgressStatus()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.InProgress);

        scenario.Task.AssignResponsible(
            scenario.Requester.Id);

        scenario.Task.MoveToTodo();
        scenario.Task.Start();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.True(
            result.IsSuccess);

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(
                    task =>
                        task.PublicId ==
                        scenario.Task.PublicId);

        Assert.Equal(
            scenario.ResponsibleUser.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.Status);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            result.Value!.Status);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldRejectAssignment_WhenProjectStatusBlocksIt(
            ProjectStatus projectStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                projectStatus);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.AssignmentBlockedByProjectStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(
                    task =>
                        task.PublicId ==
                        scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectAssignment_WhenTaskIsArchived()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning);

        scenario.Task.Cancel();
        scenario.Task.Archive();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.Archived,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(
                    task =>
                        task.PublicId ==
                        scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        Assert.True(
            persistedTask.IsArchived);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectInactiveResponsibleUser()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning);

        scenario.ResponsibleUser.Deactivate();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.ResponsibleUserInactive,
            result.Error);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectResponsibleUser_WhenMembershipWasRemoved()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning);

        scenario.ResponsibleMember.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.ResponsibleUserNotActiveMember,
            result.Error);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectTaskFromAnotherProject()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning);

        var otherProject =
            new Project(
                scenario.Tenant.Id,
                $"Outro projeto {Guid.NewGuid():N}",
                scenario.Requester.Id);

        context.Projects.Add(
            otherProject);

        await context.SaveChangesAsync();

        var otherTask =
            new ProjectTask(
                otherProject.Id,
                "Tarefa de outro projeto",
                ProjectTaskPriority.Medium,
                scenario.Requester.Id);

        context.ProjectTasks.Add(
            otherTask);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var command =
            CreateCommand(scenario) with
            {
                TaskPublicId =
                    otherTask.PublicId
            };

        var result =
            await CreateHandler(context).HandleAsync(
                command);

        Assert.Equal(
            ProjectTaskErrors.NotFound,
            result.Error);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectRequester_WhenAssignTaskPermissionWasRevoked()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member,
                ProjectStatus.Planning);

        scenario.RequesterPermission!.Revoke(
            scenario.Requester.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.AssignmentNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(
                    task =>
                        task.PublicId ==
                        scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectRequester_WhenMembershipWasRemoved()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member,
                ProjectStatus.Planning);

        scenario.RequesterMember!.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.AssignmentNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(
                    task =>
                        task.PublicId ==
                        scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldNotAccessResponsibleUserFromAnotherTenant()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning);

        var unique =
            Guid.NewGuid().ToString("N");

        var otherTenant =
            new Tenant(
                $"Outro Tenant {unique}",
                $"OTHER-{unique}",
                $"other-{unique}@test.local");

        context.Tenants.Add(
            otherTenant);

        await context.SaveChangesAsync();

        var otherUser =
            new User(
                otherTenant.Id,
                "Usuário de outro Tenant",
                $"foreign-{unique}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.Add(
            otherUser);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var command =
            CreateCommand(scenario) with
            {
                ResponsibleUserPublicId =
                    otherUser.PublicId
            };

        var result =
            await CreateHandler(context).HandleAsync(
                command);

        Assert.Equal(
            ProjectTaskErrors.ResponsibleUserNotFound,
            result.Error);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        GetForUpdateByPublicIdAsync_ShouldRequireActiveTransaction()
    {
        await using var context =
            _database.CreateDbContext();

        var repository =
            new ProjectTaskRepository(
                context);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                repository.GetForUpdateByPublicIdAsync(
                    1,
                    Guid.NewGuid()));
    }

    private static AssignProjectTaskResponsibleHandler
        CreateHandler(
            WorkFlowDbContext context)
    {
        return new AssignProjectTaskResponsibleHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private static AssignProjectTaskResponsibleCommand
        CreateCommand(
            Scenario scenario)
    {
        return new AssignProjectTaskResponsibleCommand(
            scenario.Tenant.PublicId,
            scenario.Project.PublicId,
            scenario.Task.PublicId,
            scenario.Requester.PublicId,
            scenario.ResponsibleUser.PublicId);
    }

    private static async Task<Scenario>
        CreateScenarioAsync(
            WorkFlowDbContext context,
            UserRole requesterRole,
            ProjectStatus projectStatus)
    {
        var unique =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Assign Task {unique}",
                $"REG-{unique}",
                $"tenant-{unique}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var requester =
            new User(
                tenant.Id,
                "Usuário Solicitante",
                $"requester-{unique}@test.local",
                "password-hash",
                requesterRole);

        var responsibleUser =
            new User(
                tenant.Id,
                "Usuário Responsável",
                $"responsible-{unique}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            requester,
            responsibleUser);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto Assign Task {unique}",
                requester.Id);

        SetProjectStatus(
            project,
            projectStatus);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        ProjectMember? requesterMember =
            null;

        ProjectMemberPermission? requesterPermission =
            null;

        if (requesterRole != UserRole.TenantAdmin)
        {
            requesterMember =
                new ProjectMember(
                    project.Id,
                    requester.Id,
                    requester.Id);

            context.ProjectMembers.Add(
                requesterMember);

            await context.SaveChangesAsync();

            requesterPermission =
                new ProjectMemberPermission(
                    requesterMember.Id,
                    ProjectPermission.AssignTask,
                    requester.Id);

            context.ProjectMemberPermissions.Add(
                requesterPermission);

            await context.SaveChangesAsync();
        }

        var responsibleMember =
            new ProjectMember(
                project.Id,
                responsibleUser.Id,
                requester.Id);

        context.ProjectMembers.Add(
            responsibleMember);

        await context.SaveChangesAsync();

        var task =
            new ProjectTask(
                project.Id,
                "Tarefa para atribuição",
                ProjectTaskPriority.High,
                requester.Id);

        context.ProjectTasks.Add(
            task);

        await context.SaveChangesAsync();

        return new Scenario(
            tenant,
            requester,
            responsibleUser,
            project,
            requesterMember,
            requesterPermission,
            responsibleMember,
            task);
    }

    private static void SetProjectStatus(
        Project project,
        ProjectStatus status)
    {
        switch (status)
        {
            case ProjectStatus.Planning:
                break;

            case ProjectStatus.InProgress:
                project.Start();
                break;

            case ProjectStatus.Paused:
                project.Start();

                project.Pause(
                    "Projeto pausado para teste.");

                break;

            case ProjectStatus.Completed:
                project.Start();

                project.Complete(
                    new[]
                    {
                        ProjectTaskStatus.Done
                    });

                break;

            case ProjectStatus.Archived:
                project.Archive();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
    }

    private sealed record Scenario(
        Tenant Tenant,
        User Requester,
        User ResponsibleUser,
        Project Project,
        ProjectMember? RequesterMember,
        ProjectMemberPermission? RequesterPermission,
        ProjectMember ResponsibleMember,
        ProjectTask Task);
}