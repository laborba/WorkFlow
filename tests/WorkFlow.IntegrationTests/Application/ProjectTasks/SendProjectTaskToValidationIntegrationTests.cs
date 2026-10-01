using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.SendProjectTaskToValidation;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class SendProjectTaskToValidationIntegrationTests
{
    private static readonly DateTime DueDate =
        new(
            2027,
            9,
            30,
            18,
            0,
            0,
            DateTimeKind.Utc);

    private readonly PostgreSqlTestDatabase
        _database;

    public SendProjectTaskToValidationIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database =
            database;
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldPersistValidation_WhenRequesterIsResponsibleAndActiveMember(
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
            ProjectTaskStatus.Validation,
            persistedTask.Status);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        Assert.Null(
            persistedTask.StatusBeforePause);

        Assert.Equal(
            DueDate,
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

        Assert.Null(
            result.Value.ValidatorUserPublicId);

        Assert.Equal(
            ProjectTaskStatus.Validation,
            result.Value.Status);

        Assert.Equal(
            persistedTask.UpdatedAt,
            result.Value.UpdatedAt);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectValidation_WhenMembershipWasRemoved(
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
            ProjectTaskErrors.SendToValidationNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.Status);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        Assert.Null(
            persistedTask.ValidatorUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectStatus.Planning)]
    [InlineData(ProjectStatus.Paused)]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldRejectValidation_WhenProjectIsNotInProgress(
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
                projectStatus);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors
                .SendToValidationBlockedByProjectStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectValidation_WhenTaskIsArchived()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                taskStatus:
                    ProjectTaskStatus.Cancelled);

        scenario.Task.Archive();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

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
    [InlineData(ProjectTaskStatus.Backlog)]
    [InlineData(ProjectTaskStatus.Todo)]
    [InlineData(ProjectTaskStatus.Paused)]
    [InlineData(ProjectTaskStatus.Validation)]
    [InlineData(ProjectTaskStatus.Done)]
    [InlineData(ProjectTaskStatus.Cancelled)]
    public async Task
        HandleAsync_ShouldRejectValidation_WhenTaskIsNotInProgress(
            ProjectTaskStatus taskStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context,
                taskStatus:
                    taskStatus);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors
                .SendToValidationBlockedByTaskStatus,
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
        HandleAsync_ShouldRejectValidation_WhenTaskHasNoResponsible()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context);

        context.Entry(
                scenario.Task)
            .Property(task =>
                task.ResponsibleUserId)
            .CurrentValue =
            null;

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors
                .SendToValidationRequiresResponsible,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.Status);

        Assert.Null(
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectValidation_WhenRequesterIsNotResponsible()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context);

        var otherUser =
            new User(
                scenario.Tenant.Id,
                "Outro responsável",
                $"responsible-{Guid.NewGuid():N}@test.local",
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
            ProjectTaskErrors
                .SendToValidationNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
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
                context);

        var otherProject =
            new Project(
                scenario.Tenant.Id,
                $"Outro projeto {Guid.NewGuid():N}",
                scenario.Requester.Id);

        otherProject.Start();

        context.Projects.Add(
            otherProject);

        await context.SaveChangesAsync();

        var otherTask =
            new ProjectTask(
                otherProject.Id,
                "Tarefa de outro projeto",
                ProjectTaskPriority.Medium,
                scenario.Requester.Id,
                responsibleUserId:
                    scenario.Requester.Id);

        otherTask.MoveToTodo();
        otherTask.Start();

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
                .HandleAsync(
                    command);

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
            ProjectTaskStatus.InProgress,
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
                context);

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
                SentByUserPublicId =
                    otherUser.PublicId
            };

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    command);

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
            ProjectTaskStatus.InProgress,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectValidation_WhenTenantIsInactive()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context);

        scenario.Tenant.Deactivate();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectValidation_WhenRequesterIsInactive()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario =
            await CreateScenarioAsync(
                context);

        scenario.Requester.Deactivate();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    CreateCommand(scenario));

        Assert.True(
            result.IsFailure);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.InProgress,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    private static SendProjectTaskToValidationHandler CreateHandler(
        WorkFlowDbContext context)
    {
        return new SendProjectTaskToValidationHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private static SendProjectTaskToValidationCommand CreateCommand(
        Scenario scenario)
    {
        return new SendProjectTaskToValidationCommand(
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
        UserRole requesterRole = UserRole.Member,
        ProjectStatus projectStatus =
            ProjectStatus.InProgress,
        ProjectTaskStatus taskStatus =
            ProjectTaskStatus.InProgress)
    {
        var unique =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Send Validation {unique}",
                $"REG-{unique}",
                $"tenant-{unique}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var requester =
            new User(
                tenant.Id,
                "Usuário Responsável",
                $"responsible-{unique}@test.local",
                "password-hash",
                requesterRole);

        context.Users.Add(
            requester);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto Send Validation {unique}",
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
                "Tarefa para validação",
                ProjectTaskPriority.High,
                requester.Id,
                responsibleUserId:
                    requester.Id,
                dueDate:
                    DueDate);

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

            case ProjectTaskStatus.Done:
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
        ProjectMember RequesterMember,
        ProjectTask Task);
}