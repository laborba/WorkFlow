using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.RemoveProjectTaskResponsible;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class RemoveProjectTaskResponsibleIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    public RemoveProjectTaskResponsibleIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Theory]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Planning,
        ProjectTaskStatus.Backlog,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.InProgress,
        ProjectTaskStatus.Todo,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.InProgress,
        ProjectTaskStatus.InProgress,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Paused,
        ProjectTaskStatus.Paused,
        ProjectTaskStatus.Paused)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectStatus.Planning,
        ProjectTaskStatus.Backlog,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.InProgress,
        ProjectTaskStatus.Todo,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.Planning,
        ProjectTaskStatus.Cancelled,
        ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldPersistResponsibleRemoval_WhenAuthorized(
            UserRole requesterRole,
            ProjectStatus projectStatus,
            ProjectTaskStatus taskStatus,
            ProjectTaskStatus expectedStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                requesterRole,
                projectStatus,
                taskStatus);

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
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            expectedStatus,
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

        Assert.Null(
            result.Value.ResponsibleUserPublicId);

        Assert.Equal(
            expectedStatus,
            result.Value.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldPersistTodoAsResumeStatus_WhenPausedTaskCameFromInProgress()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member,
                ProjectStatus.Paused,
                ProjectTaskStatus.Paused);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            scenario.Task.StatusBeforePause);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.True(
            result.IsSuccess);

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks
                .SingleAsync(
                    task =>
                        task.PublicId ==
                        scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            persistedTask.Status);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            persistedTask.StatusBeforePause);

        persistedTask.Resume();

        Assert.Equal(
            ProjectTaskStatus.Todo,
            persistedTask.Status);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        Assert.Null(
            persistedTask.StatusBeforePause);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldBeIdempotent_WhenTaskAlreadyHasNoResponsible()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning,
                ProjectTaskStatus.Backlog);

        scenario.Task.RemoveResponsible();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.True(
            result.IsSuccess);

        Assert.Null(
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldRejectRemoval_WhenProjectStatusBlocksIt(
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
                projectStatus,
                ProjectTaskStatus.Backlog);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors
                .ResponsibleRemovalBlockedByProjectStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectRemoval_WhenTaskIsArchived()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.Planning,
                ProjectTaskStatus.Cancelled);

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
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.True(
            persistedTask.IsArchived);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectRemoval_WhenTaskIsInValidation()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.TenantAdmin,
                ProjectStatus.InProgress,
                ProjectTaskStatus.Validation);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors
                .ResponsibleRemovalBlockedByTaskStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            persistedTask.Status);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

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
                ProjectStatus.Planning,
                ProjectTaskStatus.Backlog);

        scenario.RequesterPermission!.Revoke(
            scenario.Requester.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors
                .ResponsibleRemovalNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
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
                ProjectStatus.Planning,
                ProjectTaskStatus.Backlog);

        scenario.RequesterMember!.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors
                .ResponsibleRemovalNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectInactiveRequester()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member,
                ProjectStatus.Planning,
                ProjectTaskStatus.Backlog);

        scenario.Requester.Deactivate();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateCommand(scenario));

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

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
                ProjectStatus.Planning,
                ProjectTaskStatus.Backlog);

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

        otherTask.AssignResponsible(
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

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                otherTask.PublicId);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    private static RemoveProjectTaskResponsibleHandler
        CreateHandler(
            WorkFlowDbContext context)
    {
        return new RemoveProjectTaskResponsibleHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private static RemoveProjectTaskResponsibleCommand
        CreateCommand(
            Scenario scenario)
    {
        return new RemoveProjectTaskResponsibleCommand(
            scenario.Tenant.PublicId,
            scenario.Project.PublicId,
            scenario.Task.PublicId,
            scenario.Requester.PublicId);
    }

    private static async Task<ProjectTask>
        GetTaskAsync(
            WorkFlowDbContext context,
            Guid taskPublicId)
    {
        return await context.ProjectTasks
            .AsNoTracking()
            .SingleAsync(
                task =>
                    task.PublicId ==
                    taskPublicId);
    }

    private static async Task<Scenario>
        CreateScenarioAsync(
            WorkFlowDbContext context,
            UserRole requesterRole,
            ProjectStatus projectStatus,
            ProjectTaskStatus taskStatus)
    {
        var unique =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Remove Responsible {unique}",
                $"REMOVE-{unique}",
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

        context.Users.Add(
            requester);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto Remove Responsible {unique}",
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

        var task =
            new ProjectTask(
                project.Id,
                "Tarefa com responsável",
                ProjectTaskPriority.High,
                requester.Id);

        task.AssignResponsible(
            requester.Id);

        SetTaskStatus(
            task,
            taskStatus);

        context.ProjectTasks.Add(
            task);

        await context.SaveChangesAsync();

        return new Scenario(
            tenant,
            requester,
            project,
            requesterMember,
            requesterPermission,
            task);
    }

    private static void SetProjectStatus(
        Project project,
        ProjectStatus status)
    {
        switch (status)
        {
            case ProjectStatus.Planning:
                return;

            case ProjectStatus.InProgress:
                project.Start();
                return;

            case ProjectStatus.Paused:
                project.Start();

                project.Pause(
                    "Projeto pausado para teste.");

                return;

            case ProjectStatus.Completed:
                project.Start();

                project.Complete(
                    new[]
                    {
                        ProjectTaskStatus.Done
                    });

                return;

            case ProjectStatus.Archived:
                project.Archive();
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
    }

    private static void SetTaskStatus(
        ProjectTask task,
        ProjectTaskStatus status)
    {
        switch (status)
        {
            case ProjectTaskStatus.Backlog:
                return;

            case ProjectTaskStatus.Todo:
                task.MoveToTodo();
                return;

            case ProjectTaskStatus.InProgress:
                task.MoveToTodo();
                task.Start();
                return;

            case ProjectTaskStatus.Paused:
                task.MoveToTodo();
                task.Start();

                task.Pause(
                    "Tarefa pausada para teste.");

                return;

            case ProjectTaskStatus.Validation:
                task.MoveToTodo();
                task.Start();
                task.SendToValidation();
                return;

            case ProjectTaskStatus.Cancelled:
                task.Cancel();
                return;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status));
        }
    }

    private sealed record Scenario(
        Tenant Tenant,
        User Requester,
        Project Project,
        ProjectMember? RequesterMember,
        ProjectMemberPermission? RequesterPermission,
        ProjectTask Task);
}