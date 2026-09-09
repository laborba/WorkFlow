using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Contracts.Common;
using WorkFlow.API.Contracts.Projects;
using WorkFlow.API.Controllers;
using WorkFlow.Application.Abstractions.Persistence;
using WorkFlow.Application.Projects;
using WorkFlow.Application.Projects.AddProjectMember;
using WorkFlow.Application.Projects.ListProjectMembers;
using WorkFlow.Application.Projects.RemoveProjectMember;
using WorkFlow.Application.Tenants;
using WorkFlow.Application.Users;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Projects.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;
using WorkFlow.UnitTests.Common;

namespace WorkFlow.UnitTests.API.Projects;

public sealed class RemoveProjectMemberControllerTests
{
    [Fact]
    public async Task
    Remove_ShouldReturnOk_WhenRequesterIsTenantAdmin()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Remove(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.TargetUser.PublicId,
                CancellationToken.None);

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<RemoveProjectMemberResponse>(
                okResult.Value);

        Assert.Equal(
            fixture.Project.PublicId,
            response.ProjectPublicId);

        Assert.Equal(
            fixture.TargetUser.PublicId,
            response.UserPublicId);

        Assert.NotEqual(
            default,
            response.RemovedAt);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Remove_ShouldReturnOk_WhenRequesterIsActiveProjectManager()
    {
        var fixture =
            CreateFixture(
                UserRole.ProjectManager);

        fixture.ProjectMemberRepository
            .IsActiveMemberResult = true;

        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        var result =
            await controller.Remove(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.TargetUser.PublicId,
                CancellationToken.None);

        Assert.IsType<OkObjectResult>(
            result.Result);

        Assert.Single(
            fixture.ProjectMemberRepository
                .IsActiveMemberCalls);
    }

    [Fact]
    public async Task
    Remove_ShouldReturnUnauthorized_WhenUserClaimIsMissing()
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
            await controller.Remove(
                fixture.Tenant.PublicId,
                fixture.Project.PublicId,
                fixture.TargetUser.PublicId,
                CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(
            result.Result);

        Assert.Equal(
            0,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Remove_ShouldReturnNotFound_WhenTenantDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TenantRepository.TenantToReturn =
            null;

        var result =
            await ExecuteAsync(
                fixture);

        var notFound =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var error =
            Assert.IsType<ErrorResponse>(
                notFound.Value);

        Assert.Equal(
            TenantErrors.NotFound.Code,
            error.Code);
    }

    [Fact]
    public async Task
    Remove_ShouldReturnConflict_WhenTenantIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Tenant.Deactivate();

        var result =
            await ExecuteAsync(
                fixture);

        var conflict =
            Assert.IsType<ConflictObjectResult>(
                result.Result);

        var error =
            Assert.IsType<ErrorResponse>(
                conflict.Value);

        Assert.Equal(
            TenantErrors.Inactive.Code,
            error.Code);
    }

    [Fact]
    public async Task
    Remove_ShouldReturnForbidden_WhenRequesterIsInactive()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Requester.Deactivate();

        var result =
            await ExecuteAsync(
                fixture);

        var forbidden =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            forbidden.StatusCode);

        var error =
            Assert.IsType<ErrorResponse>(
                forbidden.Value);

        Assert.Equal(
            UserErrors.Inactive.Code,
            error.Code);
    }

    [Fact]
    public async Task
    Remove_ShouldReturnNotFound_WhenProjectDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectRepository.ProjectToReturn =
            null;

        var result =
            await ExecuteAsync(
                fixture);

        var notFound =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var error =
            Assert.IsType<ErrorResponse>(
                notFound.Value);

        Assert.Equal(
            ProjectErrors.NotFound.Code,
            error.Code);
    }

    [Fact]
    public async Task
    Remove_ShouldReturnForbidden_WhenRequesterIsMember()
    {
        var fixture =
            CreateFixture(
                UserRole.Member);

        var result =
            await ExecuteAsync(
                fixture);

        var forbidden =
            Assert.IsType<ObjectResult>(
                result.Result);

        Assert.Equal(
            StatusCodes.Status403Forbidden,
            forbidden.StatusCode);

        var error =
            Assert.IsType<ErrorResponse>(
                forbidden.Value);

        Assert.Equal(
            ProjectMemberErrors.RemoveNotAllowed.Code,
            error.Code);
    }

    [Fact]
    public async Task
    Remove_ShouldReturnNotFound_WhenTargetUserDoesNotExist()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.UserRepository
            .UsersByPublicIdToReturn
            .Remove(
                fixture.TargetUser.PublicId);

        var result =
            await ExecuteAsync(
                fixture);

        var notFound =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var error =
            Assert.IsType<ErrorResponse>(
                notFound.Value);

        Assert.Equal(
            UserErrors.NotFound.Code,
            error.Code);
    }

    [Fact]
    public async Task
    Remove_ShouldAllowInactiveTargetUser()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.TargetUser.Deactivate();

        var result =
            await ExecuteAsync(
                fixture);

        Assert.IsType<OkObjectResult>(
            result.Result);

        Assert.Equal(
            1,
            fixture.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task
    Remove_ShouldReturnNotFound_WhenTargetHasNoActiveMembership()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.ProjectMemberRepository
            .ActiveMemberToReturn = null;

        var result =
            await ExecuteAsync(
                fixture);

        var notFound =
            Assert.IsType<NotFoundObjectResult>(
                result.Result);

        var error =
            Assert.IsType<ErrorResponse>(
                notFound.Value);

        Assert.Equal(
            ProjectMemberErrors.NotActive.Code,
            error.Code);
    }

    [Fact]
    public async Task
    Remove_ShouldReturnConflict_WhenProjectIsArchived()
    {
        var fixture =
            CreateFixture(
                UserRole.TenantAdmin);

        fixture.Project.Archive();

        var result =
            await ExecuteAsync(
                fixture);

        var conflict =
            Assert.IsType<ConflictObjectResult>(
                result.Result);

        var error =
            Assert.IsType<ErrorResponse>(
                conflict.Value);

        Assert.Equal(
            ProjectErrors.Archived.Code,
            error.Code);
    }

    private static async Task<ActionResult<RemoveProjectMemberResponse>>
        ExecuteAsync(
            Fixture fixture)
    {
        var controller =
            CreateController(
                fixture);

        SetAuthenticatedUser(
            controller,
            fixture.Requester.PublicId);

        return await controller.Remove(
            fixture.Tenant.PublicId,
            fixture.Project.PublicId,
            fixture.TargetUser.PublicId,
            CancellationToken.None);
    }

    private static ProjectMembersController CreateController(
        Fixture fixture)
    {
        var addHandler =
            new AddProjectMemberHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.UnitOfWork);

        var listHandler =
            new ListProjectMembersHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository);

        var removeHandler =
            new RemoveProjectMemberHandler(
                fixture.TenantRepository,
                fixture.UserRepository,
                fixture.ProjectRepository,
                fixture.ProjectMemberRepository,
                fixture.UnitOfWork);

        return new ProjectMembersController(
            addHandler,
            listHandler,
            removeHandler);
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
                "Empresa Remove Member API",
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

        var targetUser =
            new User(
                tenant.Id,
                "Usuário Alvo",
                $"target-{Guid.NewGuid():N}@test.local",
                "password-hash",
                UserRole.Member);

        EntityTestHelper.SetId(
            targetUser,
            20);

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
            new FakeUserRepository();

        userRepository
            .UsersByPublicIdToReturn[requester.PublicId] =
                requester;

        userRepository
            .UsersByPublicIdToReturn[targetUser.PublicId] =
                targetUser;

        var projectRepository =
            new FakeProjectRepository
            {
                ProjectToReturn = project
            };

        var projectMemberRepository =
            new FakeProjectMemberRepository
            {
                ActiveMemberToReturn =
                    new ProjectMember(
                        project.Id,
                        targetUser.Id,
                        requester.Id)
            };

        var unitOfWork =
            new TestUnitOfWork();

        return new Fixture(
            tenant,
            requester,
            targetUser,
            project,
            tenantRepository,
            userRepository,
            projectRepository,
            projectMemberRepository,
            unitOfWork);
    }

    private sealed class TestUnitOfWork :
        IUnitOfWork
    {
        public int SaveChangesCallCount { get; private set; }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException(
                "Este teste não utiliza transações.");
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.FromResult(
                1);
        }
    }

    private sealed record Fixture(
        Tenant Tenant,
        User Requester,
        User TargetUser,
        Project Project,
        FakeTenantRepository TenantRepository,
        FakeUserRepository UserRepository,
        FakeProjectRepository ProjectRepository,
        FakeProjectMemberRepository ProjectMemberRepository,
        TestUnitOfWork UnitOfWork);
}