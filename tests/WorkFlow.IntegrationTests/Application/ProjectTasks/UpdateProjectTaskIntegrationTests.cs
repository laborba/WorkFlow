using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.UpdateProjectTask;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class UpdateProjectTaskIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    private static readonly DateTime OriginalDueDate =
        new(2027, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime NewDueDate =
        new(2027, 6, 15, 15, 0, 0, DateTimeKind.Utc);

    public UpdateProjectTaskIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Theory]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Planning,
        ProjectTaskStatus.Backlog)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.InProgress,
        ProjectTaskStatus.InProgress)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Paused,
        ProjectTaskStatus.Paused)]
    [InlineData(
        UserRole.TenantAdmin,
        ProjectStatus.Completed,
        ProjectTaskStatus.Cancelled)]
    [InlineData(
        UserRole.ProjectManager,
        ProjectStatus.Planning,
        ProjectTaskStatus.Todo)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.InProgress,
        ProjectTaskStatus.InProgress)]
    [InlineData(
        UserRole.Member,
        ProjectStatus.Completed,
        ProjectTaskStatus.Cancelled)]
    public async Task HandleAsync_ShouldPersistUpdate_WhenAuthorized(
        UserRole requesterRole,
        ProjectStatus projectStatus,
        ProjectTaskStatus taskStatus)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            requesterRole,
            projectStatus,
            taskStatus);

        var originalCreatorId =
            scenario.Task.CreatedByUserId;

        var originalResponsibleId =
            scenario.Task.ResponsibleUserId;

        var originalValidatorId =
            scenario.Task.ValidatorUserId;

        var originalCreatedAt =
            await context.ProjectTasks
                .AsNoTracking()
                .Where(task =>
                    task.PublicId == scenario.Task.PublicId)
                .Select(task => task.CreatedAt)
                .SingleAsync();

        var originalProjectId =
            scenario.Task.ProjectId;

        var originalStatusBeforePause =
            scenario.Task.StatusBeforePause;

        context.ChangeTracker.Clear();

        var before = DateTime.UtcNow;

        var result = await CreateHandler(context).HandleAsync(
            CreateCommand(scenario));

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        Assert.Equal(
            "Título atualizado",
            persistedTask.Title);

        Assert.Equal(
            "Descrição atualizada",
            persistedTask.Description);

        Assert.Equal(
            ProjectTaskPriority.Critical,
            persistedTask.Priority);

        Assert.Equal(
            NewDueDate,
            persistedTask.DueDate);

        Assert.Equal(
            DateTimeKind.Utc,
            persistedTask.DueDate!.Value.Kind);

        Assert.Equal(
            taskStatus,
            persistedTask.Status);

        Assert.Equal(
            originalCreatorId,
            persistedTask.CreatedByUserId);

        Assert.Equal(
            originalResponsibleId,
            persistedTask.ResponsibleUserId);

        Assert.Equal(
            originalValidatorId,
            persistedTask.ValidatorUserId);

        Assert.Equal(
            originalProjectId,
            persistedTask.ProjectId);

        Assert.Equal(
            originalCreatedAt,
            persistedTask.CreatedAt);

        Assert.Equal(
            originalStatusBeforePause,
            persistedTask.StatusBeforePause);

        Assert.NotNull(persistedTask.UpdatedAt);

        Assert.InRange(
            persistedTask.UpdatedAt!.Value,
            before,
            DateTime.UtcNow);

        var response = Assert.IsType<UpdateProjectTaskResult>(
            result.Value);

        Assert.Equal(
            scenario.Task.PublicId,
            response.PublicId);

        Assert.Equal(
            scenario.Tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            scenario.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            persistedTask.Title,
            response.Title);

        Assert.Equal(
            persistedTask.Description,
            response.Description);

        Assert.Equal(
            persistedTask.Priority,
            response.Priority);

        Assert.Equal(
            persistedTask.Status,
            response.Status);

        Assert.Equal(
            persistedTask.DueDate,
            response.DueDate);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldPersistNullDescriptionAndDueDate()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.TenantAdmin,
            ProjectStatus.Planning,
            ProjectTaskStatus.Backlog);

        context.ChangeTracker.Clear();

        var command = CreateCommand(scenario) with
        {
            Description = "   ",
            DueDate = null
        };

        var result = await CreateHandler(context).HandleAsync(
            command);

        Assert.True(result.IsSuccess);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        Assert.Equal(
            "Título atualizado",
            persistedTask.Title);

        Assert.Null(persistedTask.Description);
        Assert.Null(persistedTask.DueDate);

        Assert.Null(result.Value!.Description);
        Assert.Null(result.Value.DueDate);

        Assert.Equal(
            ProjectTaskStatus.Backlog,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldPersistLocalDueDateAsUtc()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.TenantAdmin,
            ProjectStatus.Planning,
            ProjectTaskStatus.Backlog);

        var localDate = new DateTime(
            2027,
            6,
            15,
            12,
            0,
            0,
            DateTimeKind.Local);

        var expectedUtc =
            localDate.ToUniversalTime();

        context.ChangeTracker.Clear();

        var command = CreateCommand(scenario) with
        {
            DueDate = localDate
        };

        var result = await CreateHandler(context).HandleAsync(
            command);

        Assert.True(result.IsSuccess);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        Assert.Equal(
            expectedUtc,
            persistedTask.DueDate);

        Assert.Equal(
            DateTimeKind.Utc,
            persistedTask.DueDate!.Value.Kind);

        Assert.Equal(
            expectedUtc,
            result.Value!.DueDate);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectDueDateWithoutTimezone_WithoutPersistingChanges()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.TenantAdmin,
            ProjectStatus.Planning,
            ProjectTaskStatus.Backlog);

        context.ChangeTracker.Clear();

        var command = CreateCommand(scenario) with
        {
            DueDate = new DateTime(
                2027,
                6,
                15,
                12,
                0,
                0,
                DateTimeKind.Unspecified)
        };

        await Assert.ThrowsAsync<ArgumentException>(
            () => CreateHandler(context).HandleAsync(
                command));

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        AssertOriginalTask(persistedTask);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectUpdate_WhenEditTaskPermissionWasRevoked(
        UserRole requesterRole)
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            requesterRole,
            ProjectStatus.Planning,
            ProjectTaskStatus.Backlog);

        scenario.RequesterPermission!.Revoke(
            scenario.Requester.Id);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result = await CreateHandler(context).HandleAsync(
            CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.UpdateNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        AssertOriginalTask(persistedTask);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectUpdate_WhenMembershipWasRemoved()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.Member,
            ProjectStatus.Planning,
            ProjectTaskStatus.Backlog);

        scenario.RequesterMember!.Remove();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result = await CreateHandler(context).HandleAsync(
            CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.UpdateNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        AssertOriginalTask(persistedTask);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectUpdate_WhenProjectIsArchived()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.TenantAdmin,
            ProjectStatus.Archived,
            ProjectTaskStatus.Backlog);

        context.ChangeTracker.Clear();

        var result = await CreateHandler(context).HandleAsync(
            CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.UpdateBlockedByProjectStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        AssertOriginalTask(persistedTask);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectUpdate_WhenTaskIsArchived()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.TenantAdmin,
            ProjectStatus.Planning,
            ProjectTaskStatus.Cancelled);

        scenario.Task.Archive();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result = await CreateHandler(context).HandleAsync(
            CreateCommand(scenario));

        Assert.Equal(
            ProjectTaskErrors.Archived,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        Assert.True(persistedTask.IsArchived);

        AssertOriginalTask(persistedTask);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectTaskFromAnotherProject()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.TenantAdmin,
            ProjectStatus.Planning,
            ProjectTaskStatus.Backlog);

        var otherProject = new Project(
            scenario.Tenant.Id,
            $"Outro projeto {Guid.NewGuid():N}",
            scenario.Requester.Id);

        context.Projects.Add(otherProject);

        await context.SaveChangesAsync();

        var otherTask = new ProjectTask(
            otherProject.Id,
            "Tarefa de outro projeto",
            ProjectTaskPriority.Medium,
            scenario.Requester.Id);

        context.ProjectTasks.Add(otherTask);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var command = CreateCommand(scenario) with
        {
            TaskPublicId = otherTask.PublicId
        };

        var result = await CreateHandler(context).HandleAsync(
            command);

        Assert.Equal(
            ProjectTaskErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            otherTask.PublicId);

        Assert.Equal(
            "Tarefa de outro projeto",
            persistedTask.Title);

        Assert.Equal(
            ProjectTaskPriority.Medium,
            persistedTask.Priority);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectRequesterFromAnotherTenant()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.TenantAdmin,
            ProjectStatus.Planning,
            ProjectTaskStatus.Backlog);

        var unique = Guid.NewGuid().ToString("N");

        var otherTenant = new Tenant(
            $"Outro Tenant {unique}",
            $"OTHER-{unique}",
            $"other-{unique}@test.local");

        context.Tenants.Add(otherTenant);

        await context.SaveChangesAsync();

        var otherUser = new User(
            otherTenant.Id,
            "Usuário de outro Tenant",
            $"foreign-{unique}@test.local",
            "password-hash",
            UserRole.Member);

        context.Users.Add(otherUser);

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var command = CreateCommand(scenario) with
        {
            RequestedByUserPublicId = otherUser.PublicId
        };

        var result = await CreateHandler(context).HandleAsync(
            command);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        AssertOriginalTask(persistedTask);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveTenant()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.TenantAdmin,
            ProjectStatus.Planning,
            ProjectTaskStatus.Backlog);

        scenario.Tenant.Deactivate();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result = await CreateHandler(context).HandleAsync(
            CreateCommand(scenario));

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        AssertOriginalTask(persistedTask);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveRequester()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.Member,
            ProjectStatus.Planning,
            ProjectTaskStatus.Backlog);

        scenario.Requester.Deactivate();

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var result = await CreateHandler(context).HandleAsync(
            CreateCommand(scenario));

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        AssertOriginalTask(persistedTask);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnNotFound_WhenTaskDoesNotExist()
    {
        await using var context =
            _database.CreateDbContext();

        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.TenantAdmin,
            ProjectStatus.Planning,
            ProjectTaskStatus.Backlog);

        context.ChangeTracker.Clear();

        var command = CreateCommand(scenario) with
        {
            TaskPublicId = Guid.NewGuid()
        };

        var result = await CreateHandler(context).HandleAsync(
            command);

        Assert.Equal(
            ProjectTaskErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask = await GetTaskAsync(
            context,
            scenario.Task.PublicId);

        AssertOriginalTask(persistedTask);

        await transaction.RollbackAsync();
    }

    private static UpdateProjectTaskHandler CreateHandler(
        WorkFlowDbContext context)
    {
        return new UpdateProjectTaskHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private static UpdateProjectTaskCommand CreateCommand(
        Scenario scenario)
    {
        return new UpdateProjectTaskCommand(
            scenario.Tenant.PublicId,
            scenario.Project.PublicId,
            scenario.Task.PublicId,
            scenario.Requester.PublicId,
            "  Título atualizado  ",
            "  Descrição atualizada  ",
            ProjectTaskPriority.Critical,
            NewDueDate);
    }

    private static async Task<ProjectTask> GetTaskAsync(
        WorkFlowDbContext context,
        Guid taskPublicId)
    {
        return await context.ProjectTasks
            .AsNoTracking()
            .SingleAsync(
                task => task.PublicId == taskPublicId);
    }

    private static void AssertOriginalTask(
        ProjectTask task)
    {
        Assert.Equal(
            "Título original",
            task.Title);

        Assert.Equal(
            "Descrição original",
            task.Description);

        Assert.Equal(
            ProjectTaskPriority.Medium,
            task.Priority);

        Assert.Equal(
            OriginalDueDate,
            task.DueDate);
    }

    private static async Task<Scenario> CreateScenarioAsync(
        WorkFlowDbContext context,
        UserRole requesterRole,
        ProjectStatus projectStatus,
        ProjectTaskStatus taskStatus)
    {
        var unique = Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant Update Task {unique}",
            $"REG-{unique}",
            $"tenant-{unique}@test.local");

        context.Tenants.Add(tenant);

        await context.SaveChangesAsync();

        var requester = new User(
            tenant.Id,
            "Usuário Solicitante",
            $"requester-{unique}@test.local",
            "password-hash",
            requesterRole);

        context.Users.Add(requester);

        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto Update Task {unique}",
            requester.Id);

        SetProjectStatus(
            project,
            projectStatus);

        context.Projects.Add(project);

        await context.SaveChangesAsync();

        ProjectMember? requesterMember = null;
        ProjectMemberPermission? requesterPermission = null;

        if (requesterRole != UserRole.TenantAdmin)
        {
            requesterMember = new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

            context.ProjectMembers.Add(
                requesterMember);

            await context.SaveChangesAsync();

            requesterPermission = new ProjectMemberPermission(
                requesterMember.Id,
                ProjectPermission.EditTask,
                requester.Id);

            context.ProjectMemberPermissions.Add(
                requesterPermission);

            await context.SaveChangesAsync();
        }

        var task = new ProjectTask(
            project.Id,
            "Título original",
            ProjectTaskPriority.Medium,
            requester.Id,
            description: "Descrição original",
            responsibleUserId: requester.Id,
            dueDate: OriginalDueDate);

        SetTaskStatus(
            task,
            taskStatus);

        context.ProjectTasks.Add(task);

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
                        ProjectTaskStatus.Cancelled
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