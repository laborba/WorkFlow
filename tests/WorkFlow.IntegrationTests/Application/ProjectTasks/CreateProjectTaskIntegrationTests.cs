using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.Projects;
using WorkFlow.Application.ProjectTasks;
using WorkFlow.Application.ProjectTasks.CreateProjectTask;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class CreateProjectTaskIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    public CreateProjectTaskIntegrationTests(PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.Planning, false)]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.InProgress, true)]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.Paused, true)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.Planning, true)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.InProgress, false)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.Paused, true)]
    [InlineData(UserRole.Member, ProjectStatus.Planning, true)]
    [InlineData(UserRole.Member, ProjectStatus.InProgress, true)]
    [InlineData(UserRole.Member, ProjectStatus.Paused, false)]
    public async Task HandleAsync_ShouldPersistUnassignedTask_WhenAuthorized(
        UserRole role,
        ProjectStatus status,
        bool includeOptionalFields)
    {
        await using var context = _database.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var scenario = await CreateScenarioAsync(context, role, status);
        var command = CreateCommand(scenario);
        if (!includeOptionalFields)
            command = command with { Description = null, DueDate = null };
        context.ChangeTracker.Clear();

        var result = await CreateHandler(context).HandleAsync(command);

        Assert.True(result.IsSuccess);
        context.ChangeTracker.Clear();
        var task = await context.ProjectTasks.AsNoTracking()
            .SingleAsync(task => task.PublicId == result.Value!.PublicId);
        Assert.Equal(scenario.Project.Id, task.ProjectId);
        Assert.Equal(scenario.Creator.Id, task.CreatedByUserId);
        Assert.Equal(ProjectTaskStatus.Backlog, task.Status);
        Assert.Null(task.ResponsibleUserId);
        Assert.Null(task.ValidatorUserId);
        Assert.Equal("Tarefa de integração", task.Title);
        Assert.Equal(command.Description?.Trim(), task.Description);
        Assert.Equal(command.Priority, task.Priority);
        Assert.Equal(command.DueDate, task.DueDate);
        Assert.Null(task.UpdatedAt);
        Assert.Null(task.ArchivedAt);
        Assert.Equal(scenario.Creator.PublicId, result.Value!.CreatedByUserPublicId);
        Assert.Null(result.Value.ResponsibleUserPublicId);
        // PostgreSQL armazena timestamps com precisão de microssegundos.
        var expectedCreatedAt = result.Value.CreatedAt.AddTicks(
            -(result.Value.CreatedAt.Ticks % TimeSpan.TicksPerMicrosecond));
        Assert.Equal(expectedCreatedAt, task.CreatedAt);
        var project = await context.Projects.AsNoTracking()
            .SingleAsync(project => project.Id == scenario.Project.Id);
        Assert.Equal(status, project.Status);
        Assert.Equal(1, await context.ProjectTasks.CountAsync(task => task.ProjectId == project.Id));
        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.Completed)]
    [InlineData(UserRole.TenantAdmin, ProjectStatus.Archived)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.Completed)]
    [InlineData(UserRole.ProjectManager, ProjectStatus.Archived)]
    [InlineData(UserRole.Member, ProjectStatus.Completed)]
    [InlineData(UserRole.Member, ProjectStatus.Archived)]
    public async Task HandleAsync_ShouldNotPersist_WhenProjectStatusBlocksCreation(
        UserRole role,
        ProjectStatus status)
    {
        await using var context = _database.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var scenario = await CreateScenarioAsync(context, role, status);
        context.ChangeTracker.Clear();

        var result = await CreateHandler(context).HandleAsync(CreateCommand(scenario));

        Assert.Equal(ProjectTaskErrors.CreationBlockedByProjectStatus, result.Error);
        Assert.False(await context.ProjectTasks.AnyAsync(task => task.ProjectId == scenario.Project.Id));
        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(UserRole.ProjectManager, "removed")]
    [InlineData(UserRole.Member, "removed")]
    [InlineData(UserRole.ProjectManager, "revoked")]
    [InlineData(UserRole.Member, "revoked")]
    [InlineData(UserRole.ProjectManager, "rejoined")]
    [InlineData(UserRole.Member, "rejoined")]
    public async Task HandleAsync_ShouldRejectInactiveMembershipOrPermission(
        UserRole role,
        string scenarioType)
    {
        await using var context = _database.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var scenario = await CreateScenarioAsync(context, role, ProjectStatus.Planning);
        if (scenarioType == "revoked")
        {
            scenario.Permission!.Revoke(scenario.Creator.Id);
        }
        else
        {
            scenario.Member!.Remove();
            await context.SaveChangesAsync();
            if (scenarioType == "rejoined")
                context.ProjectMembers.Add(new ProjectMember(
                    scenario.Project.Id, scenario.Creator.Id, scenario.Creator.Id));
        }
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await CreateHandler(context).HandleAsync(CreateCommand(scenario));

        Assert.Equal(ProjectTaskErrors.CreationNotAllowed, result.Error);
        Assert.False(await context.ProjectTasks.AnyAsync(task => task.ProjectId == scenario.Project.Id));
        await transaction.RollbackAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_ShouldEnforceTenantIsolation(bool useForeignProject)
    {
        await using var context = _database.CreateDbContext();
        await using var transaction = await context.Database.BeginTransactionAsync();
        var scenario = await CreateScenarioAsync(context, UserRole.TenantAdmin, ProjectStatus.Planning);
        var other = await CreateScenarioAsync(context, UserRole.TenantAdmin, ProjectStatus.Planning);
        var command = CreateCommand(scenario);
        command = useForeignProject
            ? command with { ProjectPublicId = other.Project.PublicId }
            : command with { CreatedByUserPublicId = other.Creator.PublicId };
        context.ChangeTracker.Clear();

        var result = await CreateHandler(context).HandleAsync(command);

        Assert.Equal(useForeignProject ? ProjectErrors.NotFound : UserErrors.NotFound, result.Error);
        Assert.False(await context.ProjectTasks.AnyAsync(task =>
            task.ProjectId == scenario.Project.Id || task.ProjectId == other.Project.Id));
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task
    HandleAsync_ShouldPersistUtcDueDate_WhenDueDateIsLocal()
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

        context.ChangeTracker.Clear();

        var localDueDate =
            new DateTime(
                2027,
                1,
                31,
                18,
                0,
                0,
                DateTimeKind.Local);

        var command =
            CreateCommand(
                scenario) with
            {
                DueDate = localDueDate
            };

        var result =
            await CreateHandler(context)
                .HandleAsync(
                    command);

        Assert.True(
            result.IsSuccess);

        var expectedUtc =
            localDueDate.ToUniversalTime();

        Assert.Equal(
            expectedUtc,
            result.Value!.DueDate);

        Assert.Equal(
            DateTimeKind.Utc,
            result.Value.DueDate!.Value.Kind);

        context.ChangeTracker.Clear();

        var persistedTask =
            await context.ProjectTasks
                .AsNoTracking()
                .SingleAsync(
                    task =>
                        task.PublicId ==
                        result.Value.PublicId);

        Assert.Equal(
            expectedUtc,
            persistedTask.DueDate);

        Assert.Equal(
            DateTimeKind.Utc,
            persistedTask.DueDate!.Value.Kind);

        await transaction.RollbackAsync();
    }

    private static CreateProjectTaskHandler CreateHandler(WorkFlowDbContext context)
    {
        return new CreateProjectTaskHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectMemberPermissionRepository(context),
            new ProjectTaskRepository(context),
            context);
    }

    private static CreateProjectTaskCommand CreateCommand(Scenario scenario)
    {
        return new CreateProjectTaskCommand(
            scenario.Tenant.PublicId,
            scenario.Project.PublicId,
            scenario.Creator.PublicId,
            "  Tarefa de integração  ",
            "  Descrição de integração  ",
            ProjectTaskPriority.Critical,
            new DateTime(2027, 2, 28, 12, 0, 0, DateTimeKind.Utc));
    }

    private static async Task<Scenario> CreateScenarioAsync(
        WorkFlowDbContext context,
        UserRole role,
        ProjectStatus status)
    {
        var unique = Guid.NewGuid().ToString("N");
        var tenant = new Tenant($"Tenant Tasks {unique}", $"REG-{unique}", $"tenant-{unique}@test.local");
        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var creator = new User(tenant.Id, "Criador", $"creator-{unique}@test.local", "password-hash", role);
        context.Users.Add(creator);
        await context.SaveChangesAsync();

        var project = new Project(tenant.Id, $"Projeto Tasks {unique}", creator.Id);
        switch (status)
        {
            case ProjectStatus.Planning:
                break;
            case ProjectStatus.InProgress:
                project.Start();
                break;
            case ProjectStatus.Paused:
                project.Start();
                project.Pause("Aguardando definição.");
                break;
            case ProjectStatus.Completed:
                project.Start();
                project.Complete(new[] { ProjectTaskStatus.Done });
                break;
            case ProjectStatus.Archived:
                project.Archive();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }
        context.Projects.Add(project);
        await context.SaveChangesAsync();

        ProjectMember? member = null;
        ProjectMemberPermission? permission = null;
        if (role != UserRole.TenantAdmin)
        {
            member = new ProjectMember(project.Id, creator.Id, creator.Id);
            context.ProjectMembers.Add(member);
            await context.SaveChangesAsync();
            permission = new ProjectMemberPermission(member.Id, ProjectPermission.CreateTask, creator.Id);
            context.ProjectMemberPermissions.Add(permission);
            await context.SaveChangesAsync();
        }

        return new Scenario(tenant, creator, project, member, permission);
    }

    private sealed record Scenario(
        Tenant Tenant,
        User Creator,
        Project Project,
        ProjectMember? Member,
        ProjectMemberPermission? Permission);
}
