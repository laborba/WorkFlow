using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.MoveProjectTaskToTodo;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class MoveProjectTaskToTodoIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    public MoveProjectTaskToTodoIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Theory]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Planning)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.InProgress)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Paused)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectStatus.Planning)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectStatus.InProgress)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectStatus.Paused)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.Planning)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.InProgress)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.Paused)]
    public async Task
        HandleAsync_ShouldPersistTodo_WhenAuthorized(
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
            await CreateHandler(context)
                .HandleAsync(
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

        Assert.Equal(
            ProjectTaskStatus.Todo,
            persistedTask.Status);

        Assert.Null(
            persistedTask.ResponsibleUserId);

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
            ProjectTaskStatus.Todo,
            result.Value.Status);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldReject_WhenMembershipWasRemoved(
            UserRole requesterRole)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                requesterRole,
                ProjectStatus.InProgress);

        scenario.RequesterMember!.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.MoveToTodoNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldReject_WhenEditTaskPermissionWasRevoked(
            UserRole requesterRole)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                requesterRole,
                ProjectStatus.InProgress);

        scenario.RequesterPermission!.Revoke(
            scenario.Requester.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.MoveToTodoNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldReject_WhenProjectStatusBlocksIt(
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
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.MoveToTodoBlockedByProjectStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldReject_WhenTaskIsArchived()
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

        scenario.Task.Cancel();
        scenario.Task.Archive();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
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
            ProjectTaskStatus.Cancelled,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Paused)]
    [InlineData(ProjectTaskStatus.Validation)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldReject_WhenTaskIsNotBacklog(
            ProjectTaskStatus taskStatus)
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
                taskStatus);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.MoveToTodoBlockedByTaskStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            taskStatus,
            persistedTask.Status);

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
                ProjectStatus.InProgress);

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
            await CreateHandler(context)
                .HandleAsync(command);

        Assert.Equal(
            ProjectTaskErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                otherTask.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldNotAccessRequesterFromAnotherTenant()
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
                RequestedByUserPublicId =
                    otherUser.PublicId
            };

        var result =
            await CreateHandler(context)
                .HandleAsync(command);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    private static MoveProjectTaskToTodoHandler CreateHandler(
        WorkFlowDbContext context)
    {
        return new MoveProjectTaskToTodoHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private static MoveProjectTaskToTodoCommand CreateCommand(
        Scenario scenario)
    {
        return new MoveProjectTaskToTodoCommand(
            scenario.Tenant.PublicId,
            scenario.Project.PublicId,
            scenario.Task.PublicId,
            scenario.Requester.PublicId);
    }

    private static async Task<ProjectTask> GetTaskAsync(
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

    private static async Task<Scenario> CreateScenarioAsync(
        WorkFlowDbContext context,
        UserRole requesterRole,
        ProjectStatus projectStatus,
        ProjectTaskStatus taskStatus =
            ProjectTaskStatus.Backlog)
    {
        var unique =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Move To Todo {unique}",
                $"REG-{unique}",
                $"tenant-{unique}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var requester =
            new User(
                tenant.Id,
                "Usuário Move To Todo",
                $"move-{unique}@test.local",
                "password-hash",
                requesterRole);

        context.Users.Add(
            requester);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto Move To Todo {unique}",
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
                    ProjectPermission.EditTask,
                    requester.Id);

            context.ProjectMemberPermissions.Add(
                requesterPermission);

            await context.SaveChangesAsync();
        }

        var task =
            new ProjectTask(
                project.Id,
                "Tarefa Move To Todo",
                ProjectTaskPriority.High,
                requester.Id);

        SetTaskStatus(
            task,
            taskStatus,
            requester.Id);

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
        ProjectTaskStatus status,
        long requesterUserId)
    {
        switch (status)
        {
            case ProjectTaskStatus.Backlog:
                return;

            case ProjectTaskStatus.Todo:
                task.MoveToTodo();
                return;

            case ProjectTaskStatus.InProgress:
                task.AssignResponsible(
                    requesterUserId);

                task.MoveToTodo();
                task.Start();

                return;

            case ProjectTaskStatus.Paused:
                task.MoveToTodo();

                task.Pause(
                    "Tarefa pausada para teste.");

                return;

            case ProjectTaskStatus.Validation:
                task.AssignResponsible(
                    requesterUserId);

                task.MoveToTodo();
                task.Start();
                task.SendToValidation();

                return;

            case ProjectTaskStatus.Done:
                task.AssignResponsible(
                    requesterUserId);

                task.MoveToTodo();
                task.Start();
                task.SendToValidation();
                task.ClaimValidation(
                    requesterUserId);
                task.ApproveValidation(
                    requesterUserId);

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