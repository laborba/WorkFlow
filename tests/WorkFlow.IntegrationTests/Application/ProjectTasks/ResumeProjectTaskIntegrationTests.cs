using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.ResumeProjectTask;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ResumeProjectTaskIntegrationTests
{
    private static readonly DateTime OriginalDueDate =
        new(
            2027,
            6,
            30,
            18,
            0,
            0,
            DateTimeKind.Utc);

    private static readonly DateTime NewDueDate =
        new(
            2028,
            1,
            15,
            20,
            30,
            0,
            DateTimeKind.Utc);

    private readonly PostgreSqlTestDatabase _database;

    public ResumeProjectTaskIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database =
            database;
    }

    [Theory]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectTaskStatus.InProgress)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectTaskStatus.InProgress)]
    [InlineData(
        UserRole.Member,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.Member,
        ProjectTaskStatus.InProgress)]
    public async Task
        HandleAsync_ShouldPersistResumeToPreviousStatus_WhenRequesterIsResponsibleAndActiveMember(
            UserRole requesterRole,
            ProjectTaskStatus statusBeforePause)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                requesterRole,
                statusBeforePause);

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
            statusBeforePause,
            persistedTask.Status);

        Assert.Null(
            persistedTask.StatusBeforePause);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            OriginalDueDate,
            persistedTask.DueDate);

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
            scenario.Requester.PublicId,
            result.Value.ResponsibleUserPublicId);

        Assert.Equal(
            statusBeforePause,
            result.Value.Status);

        Assert.Null(
            result.Value.StatusBeforePause);

        Assert.Equal(
            OriginalDueDate,
            result.Value.DueDate);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldPersistNewDueDate_WhenProvided()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member,
                ProjectTaskStatus.InProgress);

        context.ChangeTracker.Clear();

        var command =
            CreateCommand(scenario) with
            {
                NewDueDate =
                    NewDueDate
            };

        var result =
            await CreateHandler(context)
                .HandleAsync(command);

        Assert.True(
            result.IsSuccess);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.Status);

        Assert.Null(
            persistedTask.StatusBeforePause);

        Assert.Equal(
            NewDueDate,
            persistedTask.DueDate);

        Assert.Equal(
            DateTimeKind.Utc,
            persistedTask.DueDate!.Value.Kind);

        Assert.Equal(
            NewDueDate,
            result.Value!.DueDate);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectResume_WhenMembershipWasRemoved(
            UserRole requesterRole)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                requesterRole);

        scenario.RequesterMember.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.ResumeNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            persistedTask.Status);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.StatusBeforePause);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectStatus.Planning)]
    [InlineData(ProjectStatus.Paused)]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldRejectResume_WhenProjectIsNotInProgress(
            ProjectStatus projectStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member,
                ProjectTaskStatus.InProgress,
                projectStatus);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.ResumeBlockedByProjectStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            persistedTask.Status);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.StatusBeforePause);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectResume_WhenTaskIsArchived()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member);

        var archivedTask =
            new ProjectTask(
                scenario.Project.Id,
                "Tarefa arquivada",
                ProjectTaskPriority.Medium,
                scenario.Requester.Id,
                responsibleUserId:
                    scenario.Requester.Id);

        archivedTask.Cancel();
        archivedTask.Archive();

        context.ProjectTasks.Add(
            archivedTask);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var command =
            CreateCommand(scenario) with
            {
                TaskPublicId =
                    archivedTask.PublicId
            };

        var result =
            await CreateHandler(context)
                .HandleAsync(command);

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.Archived,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                archivedTask.PublicId);

        Assert.True(
            persistedTask.IsArchived);

        Assert.Equal(
            ProjectTaskStatus.Cancelled,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectTaskStatus.Backlog)]
    [InlineData(ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.InProgress)]
    [InlineData(ProjectTaskStatus.Validation)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldRejectResume_WhenTaskIsNotPaused(
            ProjectTaskStatus taskStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioWithCurrentStatusAsync(
                context,
                UserRole.Member,
                taskStatus);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.ResumeBlockedByTaskStatus,
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
        HandleAsync_ShouldRejectResume_WhenPausedTaskHasNoResponsible()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member,
                ProjectTaskStatus.Todo);

        scenario.Task.RemoveResponsible();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.ResumeRequiresResponsible,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            persistedTask.Status);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            persistedTask.StatusBeforePause);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectResume_WhenRequesterIsNotResponsible()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                UserRole.Member);

        var otherUser =
            new User(
                scenario.Tenant.Id,
                "Outro responsável",
                $"resume-responsible-{Guid.NewGuid():N}@test.local",
                "password-hash",
                UserRole.Member);

        context.Users.Add(
            otherUser);

        await context.SaveChangesAsync();

        scenario.Task.AssignResponsible(
            otherUser.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.ResumeNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            persistedTask.Status);

        Assert.Equal(
            otherUser.Id,
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
                UserRole.Member);

        var otherProject =
            new Project(
                scenario.Tenant.Id,
                $"Outro projeto Resume {Guid.NewGuid():N}",
                scenario.Requester.Id);

        otherProject.Start();

        context.Projects.Add(
            otherProject);

        await context.SaveChangesAsync();

        var otherTask =
            new ProjectTask(
                otherProject.Id,
                "Tarefa pausada de outro projeto",
                ProjectTaskPriority.High,
                scenario.Requester.Id,
                responsibleUserId:
                    scenario.Requester.Id);

        otherTask.MoveToTodo();
        otherTask.Start();

        otherTask.Pause(
            "Pausa em outro projeto.");

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

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                otherTask.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            persistedTask.Status);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.StatusBeforePause);

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
                UserRole.Member);

        var unique =
            Guid.NewGuid().ToString("N");

        var otherTenant =
            new Tenant(
                $"Outro Tenant Resume {unique}",
                $"OTHER-RESUME-{unique}",
                $"other-resume-{unique}@test.local");

        context.Tenants.Add(
            otherTenant);

        await context.SaveChangesAsync();

        var otherUser =
            new User(
                otherTenant.Id,
                "Usuário de outro Tenant",
                $"foreign-resume-{unique}@test.local",
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

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Paused,
            persistedTask.Status);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.StatusBeforePause);

        await transaction.RollbackAsync();
    }

    private static ResumeProjectTaskHandler CreateHandler(
        WorkFlowDbContext context)
    {
        return new ResumeProjectTaskHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private static ResumeProjectTaskCommand CreateCommand(
        Scenario scenario)
    {
        return new ResumeProjectTaskCommand(
            scenario.Tenant.PublicId,
            scenario.Project.PublicId,
            scenario.Task.PublicId,
            scenario.Requester.PublicId,
            null);
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
        ProjectTaskStatus statusBeforePause =
            ProjectTaskStatus.InProgress,
        ProjectStatus projectStatus =
            ProjectStatus.InProgress)
    {
        var scenario =
            await CreateBaseScenarioAsync(
                context,
                requesterRole,
                projectStatus);

        scenario.Task.MoveToTodo();

        if (statusBeforePause ==
            ProjectTaskStatus.InProgress)
        {
            scenario.Task.Start();
        }
        else if (statusBeforePause !=
                 ProjectTaskStatus.Todo)
        {
            throw new ArgumentOutOfRangeException(
                nameof(statusBeforePause));
        }

        scenario.Task.Pause(
            "Tarefa pausada para teste.");

        context.ProjectTasks.Add(
            scenario.Task);

        await context.SaveChangesAsync();

        return scenario;
    }

    private static async Task<Scenario>
        CreateScenarioWithCurrentStatusAsync(
            WorkFlowDbContext context,
            UserRole requesterRole,
            ProjectTaskStatus taskStatus)
    {
        var scenario =
            await CreateBaseScenarioAsync(
                context,
                requesterRole,
                ProjectStatus.InProgress);

        SetTaskStatus(
            scenario.Task,
            taskStatus);

        context.ProjectTasks.Add(
            scenario.Task);

        await context.SaveChangesAsync();

        return scenario;
    }

    private static async Task<Scenario> CreateBaseScenarioAsync(
        WorkFlowDbContext context,
        UserRole requesterRole,
        ProjectStatus projectStatus)
    {
        var unique =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Resume Task {unique}",
                $"REG-RESUME-{unique}",
                $"tenant-resume-{unique}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var requester =
            new User(
                tenant.Id,
                "Usuário Resume",
                $"resume-{unique}@test.local",
                "password-hash",
                requesterRole);

        context.Users.Add(
            requester);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto Resume Task {unique}",
                requester.Id);

        SetProjectStatus(
            project,
            projectStatus);

        context.Projects.Add(
            project);

        await context.SaveChangesAsync();

        var requesterMember =
            new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

        context.ProjectMembers.Add(
            requesterMember);

        await context.SaveChangesAsync();

        var task =
            new ProjectTask(
                project.Id,
                "Tarefa para retomar",
                ProjectTaskPriority.High,
                requester.Id,
                responsibleUserId:
                    requester.Id,
                dueDate:
                    OriginalDueDate);

        return new Scenario(
            tenant,
            requester,
            project,
            requesterMember,
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

                task.Pause(
                    "Tarefa pausada para teste.");

                return;

            case ProjectTaskStatus.Validation:
                task.MoveToTodo();
                task.Start();
                task.SendToValidation();
                return;

            case ProjectTaskStatus.Done:
                task.MoveToTodo();
                task.Start();
                task.SendToValidation();

                task.ClaimValidation(
                    task.ResponsibleUserId!.Value);

                task.ApproveValidation(
                    task.ResponsibleUserId.Value);

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
        ProjectMember RequesterMember,
        ProjectTask Task);
}