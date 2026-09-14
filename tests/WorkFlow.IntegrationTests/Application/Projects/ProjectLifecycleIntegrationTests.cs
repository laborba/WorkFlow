using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.ArchiveProject;
using WorkFlow.Application.Projects.CompleteProject;
using WorkFlow.Application.Projects.ReopenProject;
using WorkFlow.Application.Projects.RestoreProject;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.Projects;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ProjectLifecycleIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    public ProjectLifecycleIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task
    CompleteProjectHandler_ShouldPersistCompleted_WhenAllProjectTasksAreClosed()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                ProjectStatus.InProgress,
                ProjectPermission.CompleteProject);

        var doneTask =
            CreateDoneTask(
                scenario.Project.Id,
                scenario.Admin.Id);

        var cancelledTask =
            CreateCancelledTask(
                scenario.Project.Id,
                scenario.Admin.Id);

        context.ProjectTasks.AddRange(
            doneTask,
            cancelledTask);

        var unrelatedProject =
            new Project(
                scenario.Tenant.Id,
                $"Projeto não relacionado {Guid.NewGuid():N}",
                scenario.Admin.Id);

        context.Projects.Add(
            unrelatedProject);

        await context.SaveChangesAsync();

        var unrelatedOpenTask =
            new ProjectTask(
                unrelatedProject.Id,
                "Tarefa aberta de outro projeto",
                ProjectTaskPriority.Medium,
                scenario.Admin.Id);

        context.ProjectTasks.Add(
            unrelatedOpenTask);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var handler =
            CreateCompleteProjectHandler(
                context);

        var result =
            await handler.HandleAsync(
                new CompleteProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId));

        Assert.True(
            result.IsSuccess);

        Assert.Equal(
            ProjectStatus.Completed,
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
            ProjectStatus.Completed,
            persistedProject.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    CompleteProjectHandler_ShouldNotComplete_WhenProjectHasOpenTask()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                ProjectStatus.InProgress,
                ProjectPermission.CompleteProject);

        var doneTask =
            CreateDoneTask(
                scenario.Project.Id,
                scenario.Admin.Id);

        var openTask =
            new ProjectTask(
                scenario.Project.Id,
                "Tarefa ainda aberta",
                ProjectTaskPriority.Medium,
                scenario.Admin.Id);

        context.ProjectTasks.AddRange(
            doneTask,
            openTask);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var handler =
            CreateCompleteProjectHandler(
                context);

        var result =
            await handler.HandleAsync(
                new CompleteProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectErrors.HasOpenTasks,
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
            ProjectStatus.InProgress,
            persistedProject.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    ReopenProjectHandler_ShouldPersistInProgress_WhenMemberHasReopenProjectPermission()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                ProjectStatus.Completed,
                ProjectPermission.ReopenProject);

        context.ChangeTracker.Clear();

        var handler =
            CreateReopenProjectHandler(
                context);

        var result =
            await handler.HandleAsync(
                new ReopenProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId,
                    "Novos ajustes foram solicitados."));

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
    ArchiveAndRestore_ShouldRestorePlanningStatus_WhenMemberHasArchiveProjectPermission()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                ProjectStatus.Planning,
                ProjectPermission.ArchiveProject);

        context.ChangeTracker.Clear();

        var archiveHandler =
            CreateArchiveProjectHandler(
                context);

        var archiveResult =
            await archiveHandler.HandleAsync(
                new ArchiveProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId));

        Assert.True(
            archiveResult.IsSuccess);

        Assert.Equal(
            ProjectStatus.Archived,
            archiveResult.Value!.Status);

        Assert.NotNull(
            archiveResult.Value.ArchivedAt);

        context.ChangeTracker.Clear();

        var archivedProject =
            await context.Projects
                .AsNoTracking()
                .SingleAsync(
                    project =>
                        project.PublicId ==
                        scenario.Project.PublicId);

        Assert.Equal(
            ProjectStatus.Archived,
            archivedProject.Status);

        Assert.NotNull(
            archivedProject.ArchivedAt);

        context.ChangeTracker.Clear();

        var restoreHandler =
            CreateRestoreProjectHandler(
                context);

        var restoreResult =
            await restoreHandler.HandleAsync(
                new RestoreProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId));

        Assert.True(
            restoreResult.IsSuccess);

        Assert.Equal(
            ProjectStatus.Planning,
            restoreResult.Value!.Status);

        Assert.Null(
            restoreResult.Value.ArchivedAt);

        context.ChangeTracker.Clear();

        var restoredProject =
            await context.Projects
                .AsNoTracking()
                .SingleAsync(
                    project =>
                        project.PublicId ==
                        scenario.Project.PublicId);

        Assert.Equal(
            ProjectStatus.Planning,
            restoredProject.Status);

        Assert.Null(
            restoredProject.ArchivedAt);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    ArchiveAndRestore_ShouldRestoreCompletedStatus_WhenProjectWasCompleted()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                ProjectStatus.Completed,
                ProjectPermission.ArchiveProject);

        context.ChangeTracker.Clear();

        var archiveHandler =
            CreateArchiveProjectHandler(
                context);

        var archiveResult =
            await archiveHandler.HandleAsync(
                new ArchiveProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId));

        Assert.True(
            archiveResult.IsSuccess);

        Assert.Equal(
            ProjectStatus.Archived,
            archiveResult.Value!.Status);

        context.ChangeTracker.Clear();

        var restoreHandler =
            CreateRestoreProjectHandler(
                context);

        var restoreResult =
            await restoreHandler.HandleAsync(
                new RestoreProjectCommand(
                    scenario.Tenant.PublicId,
                    scenario.Project.PublicId,
                    scenario.Requester.PublicId));

        Assert.True(
            restoreResult.IsSuccess);

        Assert.Equal(
            ProjectStatus.Completed,
            restoreResult.Value!.Status);

        Assert.Null(
            restoreResult.Value.ArchivedAt);

        context.ChangeTracker.Clear();

        var persistedProject =
            await context.Projects
                .AsNoTracking()
                .SingleAsync(
                    project =>
                        project.PublicId ==
                        scenario.Project.PublicId);

        Assert.Equal(
            ProjectStatus.Completed,
            persistedProject.Status);

        Assert.Null(
            persistedProject.ArchivedAt);

        await transaction.RollbackAsync();
    }

    private static CompleteProjectHandler
        CreateCompleteProjectHandler(
            WorkFlowDbContext context)
    {
        return new CompleteProjectHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private static ReopenProjectHandler
        CreateReopenProjectHandler(
            WorkFlowDbContext context)
    {
        return new ReopenProjectHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            context);
    }

    private static ArchiveProjectHandler
        CreateArchiveProjectHandler(
            WorkFlowDbContext context)
    {
        return new ArchiveProjectHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            context);
    }

    private static RestoreProjectHandler
        CreateRestoreProjectHandler(
            WorkFlowDbContext context)
    {
        return new RestoreProjectHandler(
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
            ProjectStatus initialStatus,
            ProjectPermission permission)
    {
        var uniqueValue =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Lifecycle {uniqueValue}",
                $"REG-{uniqueValue}",
                $"tenant-{uniqueValue}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var admin =
            new User(
                tenant.Id,
                "Administrador",
                $"admin-{uniqueValue}@test.local",
                "password-hash",
                UserRole.TenantAdmin);

        var requester =
            new User(
                tenant.Id,
                "Membro Solicitante",
                $"requester-{uniqueValue}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.AddRange(
            admin,
            requester);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto Lifecycle {uniqueValue}",
                admin.Id,
                "Projeto utilizado nos testes de integração.");

        switch (initialStatus)
        {
            case ProjectStatus.Planning:
                break;

            case ProjectStatus.InProgress:
                project.Start();
                break;

            case ProjectStatus.Completed:
                project.Start();

                project.Complete(
                    new[]
                    {
                        ProjectTaskStatus.Done
                    });

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

        var projectMember =
            new ProjectMember(
                project.Id,
                requester.Id,
                admin.Id);

        context.ProjectMembers.Add(
            projectMember);

        await context.SaveChangesAsync();

        var projectMemberPermission =
            new ProjectMemberPermission(
                projectMember.Id,
                permission,
                admin.Id);

        context.ProjectMemberPermissions.Add(
            projectMemberPermission);

        await context.SaveChangesAsync();

        return new Scenario(
            tenant,
            admin,
            requester,
            project);
    }

    private static ProjectTask CreateDoneTask(
        long projectId,
        long userId)
    {
        var task =
            new ProjectTask(
                projectId,
                "Tarefa concluída",
                ProjectTaskPriority.Medium,
                userId,
                responsibleUserId: userId);

        task.MoveToTodo();
        task.Start();
        task.SendToValidation();
        task.ClaimValidation(
            userId);
        task.ApproveValidation(
            userId);

        return task;
    }

    private static ProjectTask CreateCancelledTask(
        long projectId,
        long userId)
    {
        var task =
            new ProjectTask(
                projectId,
                "Tarefa cancelada",
                ProjectTaskPriority.Medium,
                userId);

        task.Cancel();

        return task;
    }

    private sealed record Scenario(
        Tenant Tenant,
        User Admin,
        User Requester,
        Project Project);
}