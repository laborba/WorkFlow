using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.GetProjectByPublicId;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.Application.Projects.GetProjectByPublicId;

public class GetProjectByPublicIdHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldReturnProject_WhenRequesterIsTenantAdmin()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.ProjectManager);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<GetProjectByPublicIdResult>(
                result.Value);

        Assert.Equal(
            project.PublicId,
            response.PublicId);

        Assert.Equal(
            tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            creator.PublicId,
            response.CreatedByUserPublicId);

        Assert.Null(
            response.ResponsibleUserPublicId);

        Assert.Equal(
            project.Name,
            response.Name);

        Assert.Equal(
            project.Description,
            response.Description);

        Assert.Equal(
            project.Status,
            response.Status);

        Assert.Equal(
            project.DueDate,
            response.DueDate);

        Assert.Equal(
            project.CreatedAt,
            response.CreatedAt);

        Assert.Equal(
            tenant.PublicId,
            tenantRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Equal(
            requester.PublicId,
            userRepository.CheckedPublicId);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);

        Assert.Null(
            projectMemberRepository.QueriedUserId);

        Assert.Single(
            userRepository.CheckedGetByIdTenantIds);

        Assert.Single(
            userRepository.CheckedUserIds);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByIdTenantIds[0]);

        Assert.Equal(
            creator.Id,
            userRepository.CheckedUserIds[0]);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnProject_WhenRequesterIsActiveProjectManagerMember()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.ProjectManager);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.TenantAdmin);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository
            {
                IsActiveMemberResult = true
            };

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<GetProjectByPublicIdResult>(
                result.Value);

        Assert.Equal(
            project.PublicId,
            response.PublicId);

        Assert.Equal(
            creator.PublicId,
            response.CreatedByUserPublicId);

        Assert.Equal(
            project.Id,
            projectMemberRepository.QueriedProjectId);

        Assert.Equal(
            requester.Id,
            projectMemberRepository.QueriedUserId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnProject_WhenRequesterIsActiveMember()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.Member);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.TenantAdmin);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository
            {
                IsActiveMemberResult = true
            };

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<GetProjectByPublicIdResult>(
                result.Value);

        Assert.Equal(
            project.PublicId,
            response.PublicId);

        Assert.Equal(
            project.Id,
            projectMemberRepository.QueriedProjectId);

        Assert.Equal(
            requester.Id,
            projectMemberRepository.QueriedUserId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnArchivedProject_WhenRequesterIsActiveMember()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.Member);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.TenantAdmin);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        project.Archive();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository
            {
                IsActiveMemberResult = true
            };

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<GetProjectByPublicIdResult>(
                result.Value);

        Assert.Equal(
            ProjectStatus.Archived,
            response.Status);

        Assert.NotNull(
            response.ArchivedAt);

        Assert.Equal(
            project.Id,
            projectMemberRepository.QueriedProjectId);

        Assert.Equal(
            requester.Id,
            projectMemberRepository.QueriedUserId);
    }

    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "REG-GET-PROJECT",
                "empresa@getproject.test");

        EntityTestHelper.SetId(
            tenant,
            42);

        return tenant;
    }

    private static User CreatePersistedUser(
        long tenantId,
        long id,
        UserRole role)
    {
        var user =
            new User(
                tenantId,
                $"Usuário {id}",
                $"usuario{id}@getproject.test",
                "password-hash",
                role);

        EntityTestHelper.SetId(
            user,
            id);

        return user;
    }

    [Fact]
    public async Task
HandleAsync_ShouldReturnFailure_WhenTenantDoesNotExist()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid());

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            TenantErrors.NotFound,
            result.Error);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);

        Assert.Null(
            projectMemberRepository.QueriedUserId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenTenantIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        tenant.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                Guid.NewGuid(),
                Guid.NewGuid());

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            TenantErrors.Inactive,
            result.Error);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);

        Assert.Null(
            projectMemberRepository.QueriedUserId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterDoesNotExist()
    {
        var tenant =
            CreatePersistedTenant();

        var requesterPublicId =
            Guid.NewGuid();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                Guid.NewGuid(),
                requesterPublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            UserErrors.NotFound,
            result.Error);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Equal(
            requesterPublicId,
            userRepository.CheckedPublicId);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);

        Assert.Null(
            projectMemberRepository.QueriedUserId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenRequesterIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.Member);

        requester.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                Guid.NewGuid(),
                requester.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            UserErrors.Inactive,
            result.Error);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);

        Assert.Null(
            projectMemberRepository.QueriedUserId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenProjectDoesNotExist()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                Guid.NewGuid(),
                requester.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectErrors.NotFound,
            result.Error);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);

        Assert.Null(
            projectMemberRepository.QueriedUserId);
    }

    [Fact]
    public async Task
HandleAsync_ShouldReturnFailure_WhenProjectManagerIsNotActiveMember()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.ProjectManager);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.TenantAdmin);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository
            {
                IsActiveMemberResult = false
            };

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectErrors.ViewNotAllowed,
            result.Error);

        Assert.Equal(
            project.Id,
            projectMemberRepository.QueriedProjectId);

        Assert.Equal(
            requester.Id,
            projectMemberRepository.QueriedUserId);

        Assert.Empty(
            userRepository.CheckedGetByIdTenantIds);

        Assert.Empty(
            userRepository.CheckedUserIds);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldReturnFailure_WhenMemberIsNotActiveMember()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.Member);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.TenantAdmin);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository
            {
                IsActiveMemberResult = false
            };

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsFailure);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);

        Assert.Equal(
            ProjectErrors.ViewNotAllowed,
            result.Error);

        Assert.Equal(
            project.Id,
            projectMemberRepository.QueriedProjectId);

        Assert.Equal(
            requester.Id,
            projectMemberRepository.QueriedUserId);

        Assert.Empty(
            userRepository.CheckedGetByIdTenantIds);

        Assert.Empty(
            userRepository.CheckedUserIds);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenTenantPublicIdIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "TenantPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenProjectPublicIdIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid());

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "ProjectPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenRequestedByUserPublicIdIsEmpty()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty);

        var exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "RequestedByUserPublicId",
            exception.ParamName);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);
    }

    [Fact]
    public async Task
HandleAsync_ShouldReturnResponsibleUserPublicId_WhenProjectHasResponsible()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.ProjectManager);

        var responsible =
            CreatePersistedUser(
                tenant.Id,
                30,
                UserRole.Member);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id,
                responsible.Id);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        userRepository.UsersByIdToReturn.Add(
            responsible.Id,
            responsible);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId);

        var result =
            await handler.HandleAsync(query);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);

        var response =
            Assert.IsType<GetProjectByPublicIdResult>(
                result.Value);

        Assert.Equal(
            creator.PublicId,
            response.CreatedByUserPublicId);

        Assert.Equal(
            responsible.PublicId,
            response.ResponsibleUserPublicId);

        Assert.Equal(
            2,
            userRepository.CheckedUserIds.Count);

        Assert.Equal(
            creator.Id,
            userRepository.CheckedUserIds[0]);

        Assert.Equal(
            responsible.Id,
            userRepository.CheckedUserIds[1]);

        Assert.Equal(
            2,
            userRepository.CheckedGetByIdTenantIds.Count);

        Assert.All(
            userRepository.CheckedGetByIdTenantIds,
            checkedTenantId =>
                Assert.Equal(
                    tenant.Id,
                    checkedTenantId));
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenProjectCreatorCannotBeFound()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var project =
            CreatePersistedProject(
                tenant.Id,
                20);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "O usuário criador associado ao projeto não foi encontrado.",
            exception.Message);

        Assert.Single(
            userRepository.CheckedUserIds);

        Assert.Equal(
            project.CreatedByUserId,
            userRepository.CheckedUserIds[0]);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldThrow_WhenProjectResponsibleCannotBeFound()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                10,
                UserRole.TenantAdmin);

        var creator =
            CreatePersistedUser(
                tenant.Id,
                20,
                UserRole.ProjectManager);

        var project =
            CreatePersistedProject(
                tenant.Id,
                creator.Id,
                30);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = requester
            };

        userRepository.UsersByIdToReturn.Add(
            creator.Id,
            creator);

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var handler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var query =
            new GetProjectByPublicIdQuery(
                tenant.PublicId,
                project.PublicId,
                requester.PublicId);

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => handler.HandleAsync(query));

        Assert.Equal(
            "O usuário responsável associado ao projeto não foi encontrado.",
            exception.Message);

        Assert.Equal(
            2,
            userRepository.CheckedUserIds.Count);

        Assert.Equal(
            creator.Id,
            userRepository.CheckedUserIds[0]);

        Assert.Equal(
            project.ResponsibleUserId,
            userRepository.CheckedUserIds[1]);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);
    }







    private static Project CreatePersistedProject(
    long tenantId,
    long createdByUserId,
    long? responsibleUserId = null)
    {
        var project =
            new Project(
                tenantId,
                "Projeto de Teste",
                createdByUserId,
                "Descrição do projeto",
                responsibleUserId,
                new DateTime(
                    2026,
                    9,
                    30,
                    12,
                    0,
                    0,
                    DateTimeKind.Utc));

        EntityTestHelper.SetId(
            project,
            100);

        return project;
    }
}