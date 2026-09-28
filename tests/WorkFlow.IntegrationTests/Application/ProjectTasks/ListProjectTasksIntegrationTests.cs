using Microsoft.EntityFrameworkCore;
using WorkFlow.Application.ProjectTasks.ListProjectTasks;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.Infrastructure.Persistence;
using WorkFlow.Infrastructure.Persistence.Repositories;
using WorkFlow.IntegrationTests.Infrastructure;

namespace WorkFlow.IntegrationTests.Application.ProjectTasks;

[Collection(TestCollectionNames.PostgreSql)]
public sealed class ListProjectTasksIntegrationTests
{
    private readonly PostgreSqlTestDatabase _database;

    private static readonly DateTime DueDate =
        new(2027, 5, 10, 12, 0, 0, DateTimeKind.Utc);

    public ListProjectTasksIntegrationTests(
        PostgreSqlTestDatabase database)
    {
        _database = database;
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnPagedTasks_WithPublicIdentifiers()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        var second = await AddTaskAsync(
            context,
            scenario,
            "Segunda tarefa");

        var third = await AddTaskAsync(
            context,
            scenario,
            "Terceira tarefa");

        context.ChangeTracker.Clear();

        var query = CreateQuery(scenario) with
        {
            PageSize = 2
        };

        var result = await CreateHandler(context).HandleAsync(query);

        Assert.True(result.IsSuccess);

        var response = Assert.IsType<ListProjectTasksResult>(result.Value);

        Assert.Equal(1, response.PageNumber);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(3, response.TotalCount);
        Assert.Equal(2, response.TotalPages);

        Assert.Equal(
            new[] { third.PublicId, second.PublicId },
            response.Items.Select(item => item.PublicId));

        var secondPageResult =
            await CreateHandler(context).HandleAsync(
                query with { PageNumber = 2 });

        Assert.True(secondPageResult.IsSuccess);

        var secondPage =
            Assert.IsType<ListProjectTasksResult>(
                secondPageResult.Value);

        var item = Assert.Single(secondPage.Items);

        Assert.Equal(scenario.Task.PublicId, item.PublicId);
        Assert.Equal(scenario.Requester.PublicId, item.CreatedByUserPublicId);
        Assert.Equal(scenario.Requester.PublicId, item.ResponsibleUserPublicId);
        Assert.Equal(scenario.Requester.Name, item.ResponsibleUserName);
        Assert.Null(item.ValidatorUserPublicId);

        Assert.Equal("Tarefa principal", item.Title);
        Assert.Equal("Descrição original", item.Description);
        Assert.Equal(ProjectTaskStatus.Backlog, item.Status);
        Assert.Equal(ProjectTaskPriority.Medium, item.Priority);
        Assert.Equal(DueDate, item.DueDate);
        Assert.Equal(DateTimeKind.Utc, item.CreatedAt.Kind);
        Assert.Null(item.UpdatedAt);
        Assert.Null(item.ArchivedAt);

        Assert.Empty(context.ChangeTracker.Entries<ProjectTask>());
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldAllowActiveMember_WithoutEditTask(
        UserRole role)
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            role,
            addMembership: true);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario));

        Assert.True(result.IsSuccess);

        var response = Assert.IsType<ListProjectTasksResult>(result.Value);

        Assert.Single(response.Items);
        Assert.Equal(1, response.TotalCount);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectUserWithoutMembership(
        UserRole role)
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            role,
            addMembership: false);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario));

        Assert.True(result.IsFailure);
        Assert.Equal(ProjectErrors.ViewNotAllowed, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectRemovedMember()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(
            context,
            UserRole.Member,
            addMembership: true);

        scenario.Membership!.Remove();

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario));

        Assert.Equal(ProjectErrors.ViewNotAllowed, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldAllowReadingArchivedProject()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        scenario.Project.Archive();

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario));

        Assert.True(result.IsSuccess);

        var response = Assert.IsType<ListProjectTasksResult>(result.Value);

        var item = Assert.Single(response.Items);

        Assert.Equal(scenario.Task.PublicId, item.PublicId);
    }

    [Fact]
    public async Task HandleAsync_ShouldHideArchivedTasks_UnlessExplicitlyRequested()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        var archivedTask = await AddTaskAsync(
            context,
            scenario,
            "Tarefa arquivada");

        archivedTask.Cancel();
        archivedTask.Archive();

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var handler = CreateHandler(context);

        var normal =
            await handler.HandleAsync(CreateQuery(scenario));

        var explicitlyActive =
            await handler.HandleAsync(
                CreateQuery(scenario) with
                {
                    IsArchived = false
                });

        var archived =
            await handler.HandleAsync(
                CreateQuery(scenario) with
                {
                    IsArchived = true
                });

        Assert.True(normal.IsSuccess);
        Assert.True(explicitlyActive.IsSuccess);
        Assert.True(archived.IsSuccess);

        Assert.Equal(
            scenario.Task.PublicId,
            Assert.Single(normal.Value!.Items).PublicId);

        Assert.Equal(
            scenario.Task.PublicId,
            Assert.Single(explicitlyActive.Value!.Items).PublicId);

        var archivedItem =
            Assert.Single(archived.Value!.Items);

        Assert.Equal(archivedTask.PublicId, archivedItem.PublicId);
        Assert.NotNull(archivedItem.ArchivedAt);
        Assert.Equal(1, archived.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldFilterByStatus()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        var todoTask = await AddTaskAsync(
            context,
            scenario,
            "Tarefa pendente");

        todoTask.MoveToTodo();

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario) with
                {
                    Status = ProjectTaskStatus.Todo
                });

        Assert.True(result.IsSuccess);
        Assert.Equal(todoTask.PublicId, Assert.Single(result.Value!.Items).PublicId);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldFilterByPriority()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        var criticalTask = await AddTaskAsync(
            context,
            scenario,
            "Tarefa crítica",
            ProjectTaskPriority.Critical);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario) with
                {
                    Priority = ProjectTaskPriority.Critical
                });

        Assert.True(result.IsSuccess);
        Assert.Equal(
            criticalTask.PublicId,
            Assert.Single(result.Value!.Items).PublicId);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldFilterByResponsibleUser()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        var otherUser = new User(
            scenario.Tenant.Id,
            "Outro responsável",
            $"other-{Guid.NewGuid():N}@test.local",
            "password-hash",
            UserRole.Member);

        context.Users.Add(otherUser);
        await context.SaveChangesAsync();

        var otherTask = await AddTaskAsync(
            context,
            scenario,
            "Tarefa de outro responsável",
            responsibleUserId: otherUser.Id);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario) with
                {
                    ResponsibleUserPublicId = otherUser.PublicId
                });

        Assert.True(result.IsSuccess);

        var item = Assert.Single(result.Value!.Items);

        Assert.Equal(otherTask.PublicId, item.PublicId);
        Assert.Equal(otherUser.PublicId, item.ResponsibleUserPublicId);
        Assert.Equal(otherUser.Name, item.ResponsibleUserName);
        Assert.Equal(1, result.Value.TotalCount);
    }

    [Fact]
    public async Task HandleAsync_ShouldSearchTitleAndDescription_CaseInsensitive()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        var titleTask = await AddTaskAsync(
            context,
            scenario,
            "Implementar API");

        var descriptionTask = await AddTaskAsync(
            context,
            scenario,
            "Documentação",
            description: "Fluxo Secreto");

        context.ChangeTracker.Clear();

        var handler = CreateHandler(context);

        var titleResult =
            await handler.HandleAsync(
                CreateQuery(scenario) with { Search = "  api  " });

        var descriptionResult =
            await handler.HandleAsync(
                CreateQuery(scenario) with { Search = "secreto" });

        Assert.True(titleResult.IsSuccess);
        Assert.True(descriptionResult.IsSuccess);

        Assert.Equal(
            titleTask.PublicId,
            Assert.Single(titleResult.Value!.Items).PublicId);

        Assert.Equal(
            descriptionTask.PublicId,
            Assert.Single(descriptionResult.Value!.Items).PublicId);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyItems_WhenPageExceedsResults()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario) with
                {
                    PageNumber = 3,
                    PageSize = 1
                });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(1, result.Value.TotalPages);
        Assert.Equal(3, result.Value.PageNumber);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnZeroTotal_WhenNoTasksMatchFilters()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario) with
                {
                    Priority = ProjectTaskPriority.Critical
                });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectProjectFromAnotherTenant()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        var (otherTenant, otherUser) =
            await AddOtherTenantAsync(context);

        var foreignProject = new Project(
            otherTenant.Id,
            "Projeto de outro Tenant",
            otherUser.Id);

        context.Projects.Add(foreignProject);
        await context.SaveChangesAsync();

        var foreignTask = new ProjectTask(
            foreignProject.Id,
            "Tarefa de outro Tenant",
            ProjectTaskPriority.Medium,
            otherUser.Id);

        context.ProjectTasks.Add(foreignTask);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario) with
                {
                    ProjectPublicId = foreignProject.PublicId
                });

        Assert.Equal(ProjectErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectRequesterFromAnotherTenant()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        var (_, otherUser) =
            await AddOtherTenantAsync(context);

        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario) with
                {
                    RequestedByUserPublicId = otherUser.PublicId
                });

        Assert.Equal(UserErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveTenant()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        scenario.Tenant.Deactivate();

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario));

        Assert.Equal(TenantErrors.Inactive, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveRequester()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        scenario.Requester.Deactivate();

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario));

        Assert.Equal(UserErrors.Inactive, result.Error);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnValidatorPublicId()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        scenario.Task.MoveToTodo();
        scenario.Task.Start();
        scenario.Task.SendToValidation();
        scenario.Task.ClaimValidation(scenario.Requester.Id);

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario));

        Assert.True(result.IsSuccess);

        var item = Assert.Single(result.Value!.Items);

        Assert.Equal(ProjectTaskStatus.Validation, item.Status);
        Assert.Equal(scenario.Requester.PublicId, item.ValidatorUserPublicId);
    }

    [Fact]
    public async Task HandleAsync_ShouldIncludeOnlyTasksFromRequestedProject()
    {
        await using var context = _database.CreateDbContext();
        await using var transaction =
            await context.Database.BeginTransactionAsync();

        var scenario = await CreateScenarioAsync(context);

        var otherProject = new Project(
            scenario.Tenant.Id,
            "Outro projeto do mesmo Tenant",
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

        var result =
            await CreateHandler(context).HandleAsync(
                CreateQuery(scenario));

        Assert.True(result.IsSuccess);

        Assert.Equal(
            scenario.Task.PublicId,
            Assert.Single(result.Value!.Items).PublicId);

        Assert.Equal(1, result.Value.TotalCount);
    }

    private static ListProjectTasksHandler CreateHandler(
        WorkFlowDbContext context)
    {
        return new ListProjectTasksHandler(
            new TenantRepository(context),
            new UserRepository(context),
            new ProjectRepository(context),
            new ProjectMemberRepository(context),
            new ProjectTaskRepository(context));
    }

    private static ListProjectTasksQuery CreateQuery(
        Scenario scenario)
    {
        return new ListProjectTasksQuery(
            scenario.Tenant.PublicId,
            scenario.Project.PublicId,
            scenario.Requester.PublicId);
    }

    private static async Task<Scenario> CreateScenarioAsync(
        WorkFlowDbContext context,
        UserRole role = UserRole.TenantAdmin,
        bool addMembership = false)
    {
        var unique = Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Tenant List Task {unique}",
            $"REG-{unique}",
            $"tenant-{unique}@test.local");

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var requester = new User(
            tenant.Id,
            "Usuário Solicitante",
            $"requester-{unique}@test.local",
            "password-hash",
            role);

        context.Users.Add(requester);
        await context.SaveChangesAsync();

        var project = new Project(
            tenant.Id,
            $"Projeto List Task {unique}",
            requester.Id);

        context.Projects.Add(project);
        await context.SaveChangesAsync();

        ProjectMember? membership = null;

        if (addMembership)
        {
            membership = new ProjectMember(
                project.Id,
                requester.Id,
                requester.Id);

            context.ProjectMembers.Add(membership);
            await context.SaveChangesAsync();
        }

        var task = new ProjectTask(
            project.Id,
            "Tarefa principal",
            ProjectTaskPriority.Medium,
            requester.Id,
            description: "Descrição original",
            responsibleUserId: requester.Id,
            dueDate: DueDate);

        context.ProjectTasks.Add(task);
        await context.SaveChangesAsync();

        return new Scenario(
            tenant,
            requester,
            project,
            task,
            membership);
    }

    private static async Task<ProjectTask> AddTaskAsync(
        WorkFlowDbContext context,
        Scenario scenario,
        string title,
        ProjectTaskPriority priority = ProjectTaskPriority.Medium,
        string? description = null,
        long? responsibleUserId = null)
    {
        var task = new ProjectTask(
            scenario.Project.Id,
            title,
            priority,
            scenario.Requester.Id,
            description: description,
            responsibleUserId: responsibleUserId);

        context.ProjectTasks.Add(task);
        await context.SaveChangesAsync();

        return task;
    }

    private static async Task<(Tenant Tenant, User User)>
        AddOtherTenantAsync(WorkFlowDbContext context)
    {
        var unique = Guid.NewGuid().ToString("N");

        var tenant = new Tenant(
            $"Outro Tenant {unique}",
            $"OTHER-{unique}",
            $"other-{unique}@test.local");

        context.Tenants.Add(tenant);
        await context.SaveChangesAsync();

        var user = new User(
            tenant.Id,
            "Usuário de outro Tenant",
            $"foreign-{unique}@test.local",
            "password-hash",
            UserRole.TenantAdmin);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return (tenant, user);
    }

    private sealed record Scenario(
        Tenant Tenant,
        User Requester,
        Project Project,
        ProjectTask Task,
        ProjectMember? Membership);
}