using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.API.Controllers;
using WorkFlow.Application.Abstractions.Persistence.Models;
using WorkFlow.Application.Common.Pagination;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.AddProjectMember;
using WorkFlow.Application.Projects.ListProjectMembers;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.API.Projects;

public sealed class ListProjectMembersControllerTests
{
    [Fact]
    public async Task
    List_ShouldReturnPagedMembers_WhenDataIsValid()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var memberPublicId =
            Guid.NewGuid();

        var addedByPublicId =
            Guid.NewGuid();

        fixture.ProjectMemberRepository.PagedDataToReturn =
            new PagedData<ProjectMemberListItemData>(
                new[]
                {
                    new ProjectMemberListItemData(
                        memberPublicId,
                        "Membro API",
                        "membro-api@test.local",
                        UserRole.Member,
                        DateTime.UtcNow.AddDays(-1),
                        addedByPublicId)
                },
                1);

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var request =
            new ListProjectMembersRequest
            {
                PageNumber = 1,
                PageSize = 10,
                Search = "membro"
            };

        var result =
            await controller.List(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                request,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status200OK,
            okResult.StatusCode);

        var response =
            Assert.IsType<ListProjectMembersResponse>(
                okResult.Value);

        var item =
            Assert.Single(
                response.Items);

        Assert.Equal(
            memberPublicId,
            item.UserPublicId);

        Assert.Equal(
            "Membro API",
            item.Name);

        Assert.Equal(
            "membro-api@test.local",
            item.Email);

        Assert.Equal(
            UserRole.Member,
            item.Role);

        Assert.Equal(
            addedByPublicId,
            item.AddedByUserPublicId);

        Assert.Equal(
            1,
            response.PageNumber);

        Assert.Equal(
            10,
            response.PageSize);

        Assert.Equal(
            1,
            response.TotalCount);

        Assert.Equal(
            1,
            response.TotalPages);

        Assert.Equal(
            fixture.Project.Id,
            fixture.ProjectMemberRepository.LastPagedProjectId);

        Assert.Equal(
            "membro",
            fixture.ProjectMemberRepository.LastSearch);
    }

    [Fact]
    public async Task
    List_ShouldReturnUnauthorized_WhenUserPublicIdClaimIsMissing()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var controller =
            CreateController(
                fixture);

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
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new ListProjectMembersRequest(),
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
    }

    [Fact]
    public async Task
    List_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TenantRepository.TenantToReturn =
            null;

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.List(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new ListProjectMembersRequest(),
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
    }

    [Fact]
    public async Task
    List_ShouldReturnConflict_WhenTenantIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Tenant.Deactivate();

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.List(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new ListProjectMembersRequest(),
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
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Requester.Deactivate();

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.List(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new ListProjectMembersRequest(),
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
    }

    [Fact]
    public async Task
    List_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectRepository.ProjectToReturn =
            null;

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.List(
                fixture.Tenant.PublicId,
                Guid.NewGuid(),
                new ListProjectMembersRequest(),
                CancellationToken.None);

        var notFoundResult =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var response =
            Assert.IsType<ErrorResponse>(
                notFoundResult.Value);

        Assert.Equal(
            ProjectErrors.NotFound.Code,
            response.Code);
    }

    [Fact]
    public async Task
    List_ShouldReturnForbidden_WhenRequesterCannotViewProject()
    {
        var fixture =
            CreateFixture(
                UserRole.Member);

        fixture.ProjectMemberRepository
            .IsActiveMemberResult = false;

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.List(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                new ListProjectMembersRequest(),
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

        Assert.Null(
            fixture.ProjectMemberRepository.LastPagedProjectId);
    }

    private static ProjectMembersController CreateController(
        Fixture fixture)
    {
        var addProjectMemberHandler =
            new AddProjectMemberHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                new FakeUnitOfWork());

        var listProjectMembersHandler =
            new ListProjectMembersHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository);

        return new ProjectMembersController(
            addProjectMemberHandler,
            listProjectMembersHandler);
    }

    private static void SetAuthenticatedUser(
        ProjectMembersController controller,
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

    private static Fixture CreateFixture(
        UserRole requesterRole)
    {
        var tenant =
            new Tenant(
                "Empresa List Project Members API",
                $"REG-{Guid.NewGuid():N}",
                $"tenant-{Guid.NewGuid():N}@test.local");

        EntityTestHelper.SetId(
            tenant,
            42);

        var requester =
            new User(
                tenant.Id,
                "Usuário Solicitante",
                $"requester-{Guid.NewGuid():N}@test.local",
                "password-hash",
                requesterRole);

        EntityTestHelper.SetId(
            requester,
            10);

        var project =
            new Project(
                tenant.Id,
                "Projeto",
                requester.Id);

        EntityTestHelper.SetId(
            project,
            100);

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

        return new Fixture(
            tenant,
            requester,
            project,
            tenantRepository,
            userRepository,
            projectRepository,
            new FakeProjectMemberRepository());
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        Project Project,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository);
}