using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Application.ProjectTasks.ListProjectTasks;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.ProjectTasks.ListProjectTasks;

public sealed class ListProjectTasksHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldReturnPagedTasks_WhenTenantAdminIsAuthorized()
    {
        var fixture = CreateFixture();

        var taskPublicId = Guid.NewGuid();
        var responsiblePublicId = Guid.NewGuid();
        var validatorPublicId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-3);
        var updatedAt = DateTime.UtcNow.AddDays(-1);
        var dueDate = DateTime.UtcNow.AddDays(10);
        var archivedAt = DateTime.UtcNow;

        fixture.Tasks.PagedDataToReturn =
            new PagedData<ProjectTaskListItemData>(
                new[]
                {
                    new ProjectTaskListItemData(
                        taskPublicId,
                        fixture.User.PublicId,
                        true,
                        responsiblePublicId,
                        "Responsável",
                        true,
                        validatorPublicId,
                        "Tarefa API",
                        "Descrição da tarefa",
                        ProjectTaskStatus.Validation,
                        ProjectTaskPriority.High,
                        dueDate,
                        createdAt,
                        updatedAt,
                        archivedAt)
                },
                21);

        var query = fixture.Query() with
        {
            PageNumber = 2,
            PageSize = 10,
            Search = "api",
            Status = ProjectTaskStatus.Validation,
            Priority = ProjectTaskPriority.High,
            ResponsibleUserPublicId = responsiblePublicId,
            IsArchived = true
        };

        var result = await fixture.Handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);

        var response = Assert.IsType<ListProjectTasksResult>(result.Value);

        Assert.Equal(2, response.PageNumber);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(21, response.TotalCount);
        Assert.Equal(3, response.TotalPages);

        var item = Assert.Single(response.Items);

        Assert.Equal(taskPublicId, item.PublicId);
        Assert.Equal(fixture.User.PublicId, item.CreatedByUserPublicId);
        Assert.Equal(responsiblePublicId, item.ResponsibleUserPublicId);
        Assert.Equal("Responsável", item.ResponsibleUserName);
        Assert.Equal(validatorPublicId, item.ValidatorUserPublicId);
        Assert.Equal("Tarefa API", item.Title);
        Assert.Equal("Descrição da tarefa", item.Description);
        Assert.Equal(ProjectTaskStatus.Validation, item.Status);
        Assert.Equal(ProjectTaskPriority.High, item.Priority);
        Assert.Equal(dueDate, item.DueDate);
        Assert.Equal(createdAt, item.CreatedAt);
        Assert.Equal(updatedAt, item.UpdatedAt);
        Assert.Equal(archivedAt, item.ArchivedAt);

        var call = Assert.Single(fixture.Tasks.GetPagedCalls);

        Assert.Equal(fixture.Tenant.Id, call.TenantId);
        Assert.Equal(fixture.Project.Id, call.ProjectId);
        Assert.Equal(2, call.PageNumber);
        Assert.Equal(10, call.PageSize);
        Assert.Equal("api", call.Search);
        Assert.Equal(ProjectTaskStatus.Validation, call.Status);
        Assert.Equal(ProjectTaskPriority.High, call.Priority);
        Assert.Equal(responsiblePublicId, call.ResponsibleUserPublicId);
        Assert.True(call.IsArchived);

        Assert.Null(fixture.Members.QueriedProjectId);
        Assert.Null(fixture.Members.QueriedUserId);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldAllowActiveMember_WithoutEditTask(
        UserRole role)
    {
        var fixture = CreateFixture(role, isActiveMember: true);

        var result = await fixture.Handler.HandleAsync(fixture.Query());

        Assert.True(result.IsSuccess);
        Assert.Equal(fixture.Project.Id, fixture.Members.QueriedProjectId);
        Assert.Equal(fixture.User.Id, fixture.Members.QueriedUserId);
        Assert.Single(fixture.Tasks.GetPagedCalls);
    }

    [Theory]
    [InlineData(UserRole.ProjectManager)]
    [InlineData(UserRole.Member)]
    public async Task HandleAsync_ShouldRejectUserWithoutActiveMembership(
        UserRole role)
    {
        var fixture = CreateFixture(role, isActiveMember: false);

        var result = await fixture.Handler.HandleAsync(fixture.Query());

        Assert.True(result.IsFailure);
        Assert.Equal(ProjectErrors.ViewNotAllowed, result.Error);
        Assert.Empty(fixture.Tasks.GetPagedCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectSystemAdmin()
    {
        var fixture = CreateFixture(UserRole.SystemAdmin);

        var result = await fixture.Handler.HandleAsync(fixture.Query());

        Assert.Equal(ProjectErrors.ViewNotAllowed, result.Error);
        Assert.Empty(fixture.Tasks.GetPagedCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnTenantNotFound()
    {
        var fixture = CreateFixture();
        fixture.Tenants.TenantToReturn = null;

        var result = await fixture.Handler.HandleAsync(fixture.Query());

        Assert.Equal(TenantErrors.NotFound, result.Error);
        Assert.Empty(fixture.Tasks.GetPagedCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveTenant()
    {
        var fixture = CreateFixture();
        fixture.Tenant.Deactivate();

        var result = await fixture.Handler.HandleAsync(fixture.Query());

        Assert.Equal(TenantErrors.Inactive, result.Error);
        Assert.Empty(fixture.Tasks.GetPagedCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnUserNotFound()
    {
        var fixture = CreateFixture();
        fixture.Users.UserToReturn = null;

        var result = await fixture.Handler.HandleAsync(fixture.Query());

        Assert.Equal(UserErrors.NotFound, result.Error);
        Assert.Empty(fixture.Tasks.GetPagedCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldRejectInactiveUser()
    {
        var fixture = CreateFixture();
        fixture.User.Deactivate();

        var result = await fixture.Handler.HandleAsync(fixture.Query());

        Assert.Equal(UserErrors.Inactive, result.Error);
        Assert.Empty(fixture.Tasks.GetPagedCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnProjectNotFound()
    {
        var fixture = CreateFixture();
        fixture.Projects.ProjectToReturn = null;

        var result = await fixture.Handler.HandleAsync(fixture.Query());

        Assert.Equal(ProjectErrors.NotFound, result.Error);
        Assert.Empty(fixture.Tasks.GetPagedCalls);
    }

    [Fact]
    public async Task HandleAsync_ShouldThrow_WhenQueryIsNull()
    {
        var fixture = CreateFixture();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => fixture.Handler.HandleAsync(null!));

        Assert.Empty(fixture.Tasks.GetPagedCalls);
    }

    [Theory]
    [InlineData(0, "TenantPublicId")]
    [InlineData(1, "ProjectPublicId")]
    [InlineData(2, "RequestedByUserPublicId")]
    [InlineData(3, "PageNumber")]
    [InlineData(4, "PageSize")]
    [InlineData(5, "PageSize")]
    [InlineData(6, "Status")]
    [InlineData(7, "Priority")]
    [InlineData(8, "ResponsibleUserPublicId")]
    public async Task HandleAsync_ShouldRejectInvalidQuery(
        int scenario,
        string expectedParameter)
    {
        var fixture = CreateFixture();
        var validQuery = fixture.Query();

        var invalidQuery = scenario switch
        {
            0 => validQuery with { TenantPublicId = Guid.Empty },
            1 => validQuery with { ProjectPublicId = Guid.Empty },
            2 => validQuery with { RequestedByUserPublicId = Guid.Empty },
            3 => validQuery with { PageNumber = 0 },
            4 => validQuery with { PageSize = 0 },
            5 => validQuery with { PageSize = 101 },
            6 => validQuery with { Status = (ProjectTaskStatus)999 },
            7 => validQuery with { Priority = (ProjectTaskPriority)999 },
            8 => validQuery with { ResponsibleUserPublicId = Guid.Empty },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        var exception =
            await Assert.ThrowsAnyAsync<ArgumentException>(
                () => fixture.Handler.HandleAsync(invalidQuery));

        Assert.Equal(expectedParameter, exception.ParamName);
        Assert.Empty(fixture.Tasks.GetPagedCalls);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public async Task HandleAsync_ShouldDetectInconsistentUserRelations(
        int scenario)
    {
        var fixture = CreateFixture();

        var item = CreateItem(fixture.User.PublicId);

        item = scenario switch
        {
            0 => item with
            {
                CreatedByUserPublicId = null
            },
            1 => item with
            {
                HasResponsibleUser = true,
                ResponsibleUserPublicId = null,
                ResponsibleUserName = null
            },
            2 => item with
            {
                HasValidatorUser = true,
                ValidatorUserPublicId = null
            },
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        fixture.Tasks.PagedDataToReturn =
            new PagedData<ProjectTaskListItemData>(
                new[] { item },
                1);

        var expectedMessage = scenario switch
        {
            0 => "O usuário criador associado à tarefa não foi encontrado.",
            1 => "O usuário responsável associado à tarefa não foi encontrado.",
            2 => "O usuário validador associado à tarefa não foi encontrado.",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario))
        };

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => fixture.Handler.HandleAsync(fixture.Query()));

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task HandleAsync_ShouldReturnEmptyPage_WhenProjectHasNoTasks()
    {
        var fixture = CreateFixture();

        var result = await fixture.Handler.HandleAsync(fixture.Query());

        Assert.True(result.IsSuccess);

        var response = Assert.IsType<ListProjectTasksResult>(result.Value);

        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalCount);
        Assert.Equal(0, response.TotalPages);
        Assert.Single(fixture.Tasks.GetPagedCalls);
    }

    private static ProjectTaskListItemData CreateItem(
        Guid createdByUserPublicId)
    {
        return new ProjectTaskListItemData(
            Guid.NewGuid(),
            createdByUserPublicId,
            false,
            null,
            null,
            false,
            null,
            "Tarefa de teste",
            null,
            ProjectTaskStatus.Backlog,
            ProjectTaskPriority.Medium,
            null,
            DateTime.UtcNow,
            null,
            null);
    }

    private static Fixture CreateFixture(
        UserRole role = UserRole.TenantAdmin,
        bool isActiveMember = true)
    {
        var tenant = new Tenant(
            "Empresa de Teste",
            $"REG-{Guid.NewGuid():N}",
            $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(tenant, 42);

        long? userTenantId =
            role == UserRole.SystemAdmin
                ? null
                : tenant.Id;

        var user = new User(
            userTenantId,
            "Usuário de Teste",
            $"usuario-{Guid.NewGuid():N}@test.local",
            "password-hash",
            role);

        EntityTestHelper.SetId(user, 84);

        var project = new Project(
            tenant.Id,
            "Projeto de Teste",
            user.Id);

        EntityTestHelper.SetId(project, 126);

        var tenants = new FakeTenantRepository
        {
            TenantToReturn = tenant
        };

        var users = new FakeUserRepository
        {
            UserToReturn = user
        };

        var projects = new FakeProjectRepository
        {
            ProjectToReturn = project
        };

        var members = new FakeProjectMemberRepository
        {
            IsActiveMemberResult = isActiveMember
        };

        var tasks = new FakeProjectTaskRepository();

        var handler = new ListProjectTasksHandler(
            tenants,
            users,
            projects,
            members,
            tasks);

        return new Fixture(
            tenant,
            user,
            project,
            tenants,
            users,
            projects,
            members,
            tasks,
            handler);
    }

    private sealed record Fixture(
        Tenant Tenant,
        User User,
        Project Project,
        FakeTenantRepository Tenants,
        FakeUserRepository Users,
        FakeProjectRepository Projects,
        FakeProjectMemberRepository Members,
        FakeProjectTaskRepository Tasks,
        ListProjectTasksHandler Handler)
    {
        public ListProjectTasksQuery Query()
        {
            return new ListProjectTasksQuery(
                Tenant.PublicId,
                Project.PublicId,
                User.PublicId);
        }
    }
}