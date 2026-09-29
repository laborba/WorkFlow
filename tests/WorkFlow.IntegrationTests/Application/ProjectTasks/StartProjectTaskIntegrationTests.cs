using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.StartProjectTask;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class StartProjectTaskIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    public StartProjectTaskIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldPersistStart_WhenRequesterIsResponsibleAndActiveMember(
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

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

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
            ProjectTaskStatus.InProgress,
            result.Value.Status);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin)]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task
        HandleAsync_ShouldRejectStart_WhenMembershipWasRemoved(
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

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.StartNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            persistedTask.Status);

        Assert.Equal(
            scenario.Requester.Id,
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(ProjectStatus.Planning)]
    [InlineData(ProjectStatus.Paused)]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Archived)]
    public async Task
        HandleAsync_ShouldRejectStart_WhenProjectIsNotInProgress(
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

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.StartBlockedByProjectStatus,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Todo,
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
                UserRole.Member);

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

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                otherTask.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Todo,
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
                UserRole.Member);

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
                StartedByUserPublicId =
                    otherUser.PublicId
            };

        var result =
            await CreateHandler(context)
                .HandleAsync(command);

        Assert.True(result.IsFailure);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            persistedTask.Status);

        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
        HandleAsync_ShouldRejectStart_WhenRequesterIsNotResponsible()
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

        Assert.True(result.IsFailure);

        Assert.Equal(
            ProjectTaskErrors.StartNotAllowed,
            result.Error);

        context.ChangeTracker.Clear();

        var persistedTask =
            await GetTaskAsync(
                context,
                scenario.Task.PublicId);

        Assert.Equal(
            ProjectTaskStatus.Todo,
            persistedTask.Status);

        Assert.Equal(
            otherUser.Id,
            persistedTask.ResponsibleUserId);

        await transaction.RollbackAsync();
    }

    private static StartProjectTaskHandler CreateHandler(
        WorkFlowDbContext context)
    {
        return new StartProjectTaskHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private static StartProjectTaskCommand CreateCommand(
        Scenario scenario)
    {
        return new StartProjectTaskCommand(
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
        ProjectStatus projectStatus =
            ProjectStatus.InProgress)
    {
        var unique =
            Guid.NewGuid().ToString("N");

        var tenant =
            new Tenant(
                $"Tenant Start Task {unique}",
                $"REG-{unique}",
                $"tenant-{unique}@test.local");

        context.Tenants.Add(
            tenant);

        await context.SaveChangesAsync();

        var requester =
            new User(
                tenant.Id,
                "Usuário Start",
                $"start-{unique}@test.local",
                "password-hash",
                requesterRole);

        context.Users.Add(
            requester);

        await context.SaveChangesAsync();

        var project =
            new Project(
                tenant.Id,
                $"Projeto Start Task {unique}",
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
                "Tarefa pronta para iniciar",
                ProjectTaskPriority.High,
                requester.Id,
                responsibleUserId:
                    requester.Id);

        task.MoveToTodo();

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

    private sealed record Scenario(
        Tenant Tenant,
        User Requester,
        Project Project,
        ProjectMember RequesterMember,
        ProjectTask Task);
}