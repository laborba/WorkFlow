using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.API.Controllers;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.CreateProject;
using WorkFlow.Application.Projects.GetProjectByPublicId;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.API.Projects;

public sealed class ProjectsControllerTests
{
    [Fact]
    public async Task
    Create_ShouldUseAuthenticatedUserAsCreator_WhenDataIsValid()
    {
        var tenant =
            CreatePersistedTenant();

        var creator =
            CreatePersistedUser(
                tenant.Id,
                UserRole.TenantAdmin);

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var userRepository =
            new FakeUserRepository
            {
                UserToReturn = creator
            };

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var unitOfWork =
            new FakeUnitOfWork();

        unitOfWork.OnSaveChanges =
            saveChangesCallCount =>
            {
                if (saveChangesCallCount == 1)
                {
                    var project =
                        Assert.IsType<Project>(
                            projectRepository.AddedProject);

                    EntityTestHelper.SetId(
                        project,
                        100);
                }
            };

        var handler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                unitOfWork);

        var getProjectByPublicIdHandler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var controller =
            new ProjectsController(
                handler,
                getProjectByPublicIdHandler);

        var identity =
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        creator.PublicId.ToString())
                },
                "Test");

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                identity)
                    }
            };

        var dueDate =
            new DateTime(
                2026,
                9,
                30,
                12,
                0,
                0,
                DateTimeKind.Utc);

        var request =
            new CreateProjectRequest(
                "Projeto de Teste",
                "Descrição do projeto",
                dueDate);

        var result =
            await controller.Create(
                tenant.PublicId,
                request,
                CancellationToken.None);

        var createdResult =
            Assert.IsType<CreatedResult>(
                result.Result);

        var response =
            Assert.IsType<CreateProjectResponse>(
                createdResult.Value);

        Assert.Equal(
            creator.PublicId,
            userRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

        var addedProject =
            Assert.IsType<Project>(
                projectRepository.AddedProject);

        Assert.Equal(
            creator.Id,
            addedProject.CreatedByUserId);

        Assert.Equal(
            creator.PublicId,
            response.CreatedByUserPublicId);

        Assert.Equal(
            tenant.PublicId,
            response.TenantPublicId);

        Assert.Equal(
            request.Name,
            response.Name);

        Assert.Equal(
            request.Description,
            response.Description);

        Assert.Equal(
            dueDate,
            response.DueDate);

        Assert.Equal(
            ProjectStatus.Planning,
            response.Status);

        Assert.Equal(
            $"/api/tenants/" +
            $"{tenant.PublicId}/projects/" +
            $"{response.PublicId}",
            createdResult.Location);
    }

    [Fact]
    public async Task
    Create_ShouldReturnUnauthorized_WhenUserPublicIdClaimIsMissing()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var createProjectHandler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeUnitOfWork());

        var getProjectByPublicIdHandler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var controller =
            new ProjectsController(
                createProjectHandler,
                getProjectByPublicIdHandler);
    }

    [Fact]
    public async Task
GetByPublicId_ShouldUseAuthenticatedUserAndReturnProject_WhenDataIsValid()
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

        var createProjectHandler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeUnitOfWork());

        var getProjectByPublicIdHandler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var controller =
            new ProjectsController(
                createProjectHandler,
                getProjectByPublicIdHandler);

        var identity =
            new ClaimsIdentity(
                new[]
                {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    requester.PublicId.ToString())
                },
                "Test");

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                identity)
                    }
            };

        var result =
            await controller.GetByPublicId(
                tenant.PublicId,
                project.PublicId,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<GetProjectByPublicIdResponse>(
                okResult.Value);

        Assert.Equal(
            StatusCodes.Status200OK,
            okResult.StatusCode);

        Assert.Equal(
            requester.PublicId,
            userRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            userRepository.CheckedGetByPublicIdTenantId);

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

        Assert.Null(
            projectMemberRepository.QueriedProjectId);

        Assert.Null(
            projectMemberRepository.QueriedUserId);
    }


    [Fact]
    public async Task
    GetByPublicId_ShouldReturnUnauthorized_WhenUserPublicIdClaimIsMissing()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var projectMemberRepository =
            new FakeProjectMemberRepository();

        var createProjectHandler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeUnitOfWork());

        var getProjectByPublicIdHandler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var controller =
            new ProjectsController(
                createProjectHandler,
                getProjectByPublicIdHandler);

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                new ClaimsIdentity(
                                    authenticationType: "Test"))
                    }
            };

        var result =
            await controller.GetByPublicId(
                Guid.NewGuid(),
                Guid.NewGuid(),
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);

        Assert.Null(
            projectMemberRepository.QueriedProjectId);
    }

    [Fact]
    public async Task
GetByPublicId_ShouldReturnNotFound_WhenProjectDoesNotExist()
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

        var createProjectHandler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeUnitOfWork());

        var getProjectByPublicIdHandler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var controller =
            new ProjectsController(
                createProjectHandler,
                getProjectByPublicIdHandler);

        var identity =
            new ClaimsIdentity(
                new[]
                {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    requester.PublicId.ToString())
                },
                "Test");

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                identity)
                    }
            };

        var result =
            await controller.GetByPublicId(
                tenant.PublicId,
                Guid.NewGuid(),
                CancellationToken.None);

        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                notFoundResult.Value);

        Assert.Equal(
            StatusCodes.Status404NotFound,
            notFoundResult.StatusCode);

        Assert.Equal(
            ProjectErrors.NotFound.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.NotFound.Message,
            response.Message);
    }

    [Fact]
    public async Task
GetByPublicId_ShouldReturnForbidden_WhenRequesterCannotViewProject()
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

        var createProjectHandler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeUnitOfWork());

        var getProjectByPublicIdHandler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var controller =
            new ProjectsController(
                createProjectHandler,
                getProjectByPublicIdHandler);

        var identity =
            new ClaimsIdentity(
                new[]
                {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    requester.PublicId.ToString())
                },
                "Test");

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                identity)
                    }
            };

        var result =
            await controller.GetByPublicId(
                tenant.PublicId,
                project.PublicId,
                CancellationToken.None);

        var forbiddenResult =
            Assert.IsType<ObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                forbiddenResult.Value);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            forbiddenResult.StatusCode);

        Assert.Equal(
            ProjectErrors.ViewNotAllowed.Code,
            response.Code);

        Assert.Equal(
            ProjectErrors.ViewNotAllowed.Message,
            response.Message);

        Assert.Equal(
            project.Id,
            projectMemberRepository.QueriedProjectId);

        Assert.Equal(
            requester.Id,
            projectMemberRepository.QueriedUserId);
    }

    [Fact]
    public async Task
GetByPublicId_ShouldReturnConflict_WhenTenantIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        tenant.Deactivate();

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

        var createProjectHandler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeUnitOfWork());

        var getProjectByPublicIdHandler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var controller =
            new ProjectsController(
                createProjectHandler,
                getProjectByPublicIdHandler);

        var identity =
            new ClaimsIdentity(
                new[]
                {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    requesterPublicId.ToString())
                },
                "Test");

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                identity)
                    }
            };

        var result =
            await controller.GetByPublicId(
                tenant.PublicId,
                Guid.NewGuid(),
                CancellationToken.None);

        var conflictResult =
            Assert.IsType<ConflictObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                conflictResult.Value);

        Assert.Equal(
            StatusCodes.Status409Conflict,
            conflictResult.StatusCode);

        Assert.Equal(
            TenantErrors.Inactive.Code,
            response.Code);

        Assert.Equal(
            TenantErrors.Inactive.Message,
            response.Message);

        Assert.Null(
            userRepository.CheckedGetByPublicIdTenantId);
    }

    [Fact]
    public async Task
GetByPublicId_ShouldReturnForbidden_WhenRequesterIsInactive()
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

        var createProjectHandler =
            new CreateProjectHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository,
                new FakeUnitOfWork());

        var getProjectByPublicIdHandler =
            new GetProjectByPublicIdHandler(
                tenantRepository,
                userRepository,
                projectRepository,
                projectMemberRepository);

        var controller =
            new ProjectsController(
                createProjectHandler,
                getProjectByPublicIdHandler);

        var identity =
            new ClaimsIdentity(
                new[]
                {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    requester.PublicId.ToString())
                },
                "Test");

        controller.ControllerContext =
            new ControllerContext
            {
                HttpContext =
                    new DefaultHttpContext
                    {
                        User =
                            new ClaimsPrincipal(
                                identity)
                    }
            };

        var result =
            await controller.GetByPublicId(
                tenant.PublicId,
                Guid.NewGuid(),
                CancellationToken.None);

        var forbiddenResult =
            Assert.IsType<ObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                forbiddenResult.Value);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            forbiddenResult.StatusCode);

        Assert.Equal(
            UserErrors.Inactive.Code,
            response.Code);

        Assert.Equal(
            UserErrors.Inactive.Message,
            response.Message);
    }



    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa de Teste",
                "REG-PROJECT-CONTROLLER",
                "empresa@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        return tenant;
    }

    private static User CreatePersistedUser(
        long tenantId,
        UserRole role)
    {
        var user =
            new User(
                tenantId,
                "Usuário Criador",
                "criador@test.local",
                "password-hash",
                role);

        EntityTestHelper.SetId(
            user,
            84);

        return user;
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
                $"usuario{id}@project-controller.test",
                "password-hash",
                role);

        EntityTestHelper.SetId(
            user,
            id);

        return user;
    }

    private static Project CreatePersistedProject(
        long tenantId,
        long createdByUserId)
    {
        var project =
            new Project(
                tenantId,
                "Projeto de Teste",
                createdByUserId,
                "Descrição do projeto",
                dueDate: new DateTime(
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