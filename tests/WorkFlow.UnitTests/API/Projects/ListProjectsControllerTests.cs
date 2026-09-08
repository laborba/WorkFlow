using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.API.Controllers;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Application.Projects.CreateProject;
using WorkFlow.Application.Projects.GetProjectByPublicId;
using WorkFlow.Application.Projects.ListProjects;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.API.Projects;

public sealed class ListProjectsControllerTests
{
    [Fact]
    public async Task
    List_ShouldUseAuthenticatedUserAndReturnPagedProjects_WhenDataIsValid()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
                UserRole.TenantAdmin);

        var responsiblePublicId =
            Guid.NewGuid();

        var projectPublicId =
            Guid.NewGuid();

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
                PagedDataToReturn =
                    new PagedData<ProjectListItemData>(
                        new[]
                        {
                            new ProjectListItemData(
                                projectPublicId,
                                true,
                                responsiblePublicId,
                                "Responsável",
                                "Projeto API",
                                "Descrição",
                                ProjectStatus.InProgress,
                                DateTime.UtcNow.AddDays(10),
                                DateTime.UtcNow.AddDays(-5),
                                null,
                                null)
                        },
                        1)
            };

        var controller =
            CreateController(
                tenantRepository,
                userRepository,
                projectRepository);

        SetAuthenticatedUser(
            controller,
            requester.PublicId);

        var request =
            new ListProjectsRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Search = "api",
                Status = (int)ProjectStatus.InProgress,
                ResponsibleUserPublicId =
                    responsiblePublicId
            };

        var result =
            await controller.List(
                tenant.PublicId,
                request,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ListProjectsResponse>(
                okResult.Value);

        Assert.Equal(
            StatusCodes.Status200OK,
            okResult.StatusCode);

        var item =
            Assert.Single(response.Items);

        Assert.Equal(
            projectPublicId,
            item.PublicId);

        Assert.Equal(
            "Projeto API",
            item.Name);

        Assert.Equal(
            ProjectStatus.InProgress,
            item.Status);

        Assert.Equal(
            responsiblePublicId,
            item.ResponsibleUserPublicId);

        Assert.Equal(
            "Responsável",
            item.ResponsibleUserName);

        Assert.Equal(
            1,
            response.TotalCount);

        Assert.Equal(
            requester.PublicId,
            userRepository.CheckedPublicId);

        Assert.Equal(
            tenant.Id,
            projectRepository.LastTenantId);

        Assert.Equal(
            "api",
            projectRepository.LastSearch);

        Assert.Equal(
            ProjectStatus.InProgress,
            projectRepository.LastStatus);

        Assert.Equal(
            responsiblePublicId,
            projectRepository.LastResponsibleUserPublicId);
    }

    [Fact]
    public async Task
    List_ShouldReturnUnauthorized_WhenUserPublicIdClaimIsMissing()
    {
        var tenantRepository =
            new FakeTenantRepository();

        var userRepository =
            new FakeUserRepository();

        var projectRepository =
            new FakeProjectRepository();

        var controller =
            CreateController(
                tenantRepository,
                userRepository,
                projectRepository);

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
            await controller.List(
                Guid.NewGuid(),
                new ListProjectsRequest(),
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Null(
            tenantRepository.CheckedPublicId);

        Assert.Null(
            projectRepository.LastTenantId);
    }

    [Fact]
    public async Task
    List_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var controller =
            CreateController(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakeProjectRepository());

        SetAuthenticatedUser(
            controller,
            Guid.NewGuid());

        var result =
            await controller.List(
                Guid.NewGuid(),
                new ListProjectsRequest(),
                CancellationToken.None);

        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                notFoundResult.Value);

        Assert.Equal(
            TenantErrors.NotFound.Code,
            response.Code);

        Assert.Equal(
            TenantErrors.NotFound.Message,
            response.Message);
    }

    [Fact]
    public async Task
    List_ShouldReturnConflict_WhenTenantIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        tenant.Deactivate();

        var tenantRepository =
            new FakeTenantRepository
            {
                TenantToReturn = tenant
            };

        var controller =
            CreateController(
                tenantRepository,
                new FakeUserRepository(),
                new FakeProjectRepository());

        SetAuthenticatedUser(
            controller,
            Guid.NewGuid());

        var result =
            await controller.List(
                tenant.PublicId,
                new ListProjectsRequest(),
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
    }

    [Fact]
    public async Task
    List_ShouldReturnForbidden_WhenRequesterIsInactive()
    {
        var tenant =
            CreatePersistedTenant();

        var requester =
            CreatePersistedUser(
                tenant.Id,
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

        var controller =
            CreateController(
                tenantRepository,
                userRepository,
                new FakeProjectRepository());

        SetAuthenticatedUser(
            controller,
            requester.PublicId);

        var result =
            await controller.List(
                tenant.PublicId,
                new ListProjectsRequest(),
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

    private static ProjectsController CreateController(
        FakeTenantRepository tenantRepository,
        FakeUserRepository userRepository,
        FakeProjectRepository projectRepository)
    {
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

        var listProjectsHandler =
            new ListProjectsHandler(
                tenantRepository,
                userRepository,
                projectRepository);

        return new ProjectsController(
            createProjectHandler,
            getProjectByPublicIdHandler,
            listProjectsHandler);
    }

    private static void SetAuthenticatedUser(
        ProjectsController controller,
        Guid userPublicId)
    {
        var identity =
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        userPublicId.ToString())
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
    }

    private static Tenant CreatePersistedTenant()
    {
        var tenant =
            new Tenant(
                "Empresa List Projects",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

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
                "Usuário List Projects",
                $"usuario-{Guid.NewGuid():N}@test.local",
                "password-hash",
                role);

        EntityTestHelper.SetId(
            user,
            84);

        return user;
    }
}