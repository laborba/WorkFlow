using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WorkFlow.API.Contracts.Authentication;
using WorkFlow.API.Controllers;
using WorkFlow.Application.Authentication.Login;
using WorkFlow.Domain.Enums;
using WorkFlow.UnitTests.Application.Authentication.Fakes;
using WorkFlow.UnitTests.Application.Tenants.Fakes;
using WorkFlow.UnitTests.Application.Users.Fakes;

namespace WorkFlow.UnitTests.API.Authentication;

public sealed class AuthenticationControllerTests
{
    [Fact]
    public void
    Me_ShouldReturnSystemAdminWithoutTenant_WhenClaimsAreValid()
    {
        var loginHandler =
            new LoginHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakePasswordHasher(),
                new FakeAccessTokenGenerator());

        var controller =
            new AuthenticationController(
                loginHandler);

        var userPublicId =
            Guid.NewGuid();

        var identity =
            new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        userPublicId.ToString()),

                    new Claim(
                        ClaimTypes.Name,
                        "Administrador do Sistema"),

                    new Claim(
                        ClaimTypes.Email,
                        "admin@workflow.test"),

                    new Claim(
                        ClaimTypes.Role,
                        UserRole.SystemAdmin.ToString())
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
            controller.Me();

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<CurrentUserResponse>(
                okResult.Value);

        Assert.Equal(
            userPublicId,
            response.UserPublicId);

        Assert.Null(
            response.TenantPublicId);

        Assert.Equal(
            "Administrador do Sistema",
            response.Name);

        Assert.Equal(
            "admin@workflow.test",
            response.Email);

        Assert.Equal(
            UserRole.SystemAdmin,
            response.Role);
    }

    [Fact]
    public void
    Me_ShouldReturnUnauthorized_WhenSystemAdminHasTenantClaim()
    {
        var loginHandler =
            new LoginHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakePasswordHasher(),
                new FakeAccessTokenGenerator());

        var controller =
            new AuthenticationController(
                loginHandler);

        var identity =
            new ClaimsIdentity(
                new[]
                {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    Guid.NewGuid().ToString()),

                new Claim(
                    ClaimTypes.Name,
                    "Administrador do Sistema"),

                new Claim(
                    ClaimTypes.Email,
                    "admin@workflow.test"),

                new Claim(
                    ClaimTypes.Role,
                    UserRole.SystemAdmin.ToString()),

                new Claim(
                    WorkFlow.API.Authentication.JwtClaimNames.TenantPublicId,
                    Guid.NewGuid().ToString())
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
            controller.Me();

        Assert.IsType<UnauthorizedResult>(
            result.Result);
    }

    [Fact]
    public void
    Me_ShouldReturnUnauthorized_WhenTenantUserDoesNotHaveTenantClaim()
    {
        var loginHandler =
            new LoginHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakePasswordHasher(),
                new FakeAccessTokenGenerator());

        var controller =
            new AuthenticationController(
                loginHandler);

        var identity =
            new ClaimsIdentity(
                new[]
                {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    Guid.NewGuid().ToString()),

                new Claim(
                    ClaimTypes.Name,
                    "Usuário de Tenant"),

                new Claim(
                    ClaimTypes.Email,
                    "usuario@workflow.test"),

                new Claim(
                    ClaimTypes.Role,
                    UserRole.Member.ToString())
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
            controller.Me();

        Assert.IsType<UnauthorizedResult>(
            result.Result);
    }

    [Fact]
    public void
    Me_ShouldReturnTenantUser_WhenClaimsAreValid()
    {
        var loginHandler =
            new LoginHandler(
                new FakeTenantRepository(),
                new FakeUserRepository(),
                new FakePasswordHasher(),
                new FakeAccessTokenGenerator());

        var controller =
            new AuthenticationController(
                loginHandler);

        var userPublicId =
            Guid.NewGuid();

        var tenantPublicId =
            Guid.NewGuid();

        var identity =
            new ClaimsIdentity(
                new[]
                {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    userPublicId.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    "Usuário de Tenant"),

                new Claim(
                    ClaimTypes.Email,
                    "usuario@workflow.test"),

                new Claim(
                    ClaimTypes.Role,
                    UserRole.Member.ToString()),

                new Claim(
                    WorkFlow.API.Authentication.JwtClaimNames.TenantPublicId,
                    tenantPublicId.ToString())
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
            controller.Me();

        var okResult =
            Assert.IsType<OkObjectResult>(
                result.Result);

        var response =
            Assert.IsType<CurrentUserResponse>(
                okResult.Value);

        Assert.Equal(
            userPublicId,
            response.UserPublicId);

        Assert.Equal(
            tenantPublicId,
            response.TenantPublicId);

        Assert.Equal(
            "Usuário de Tenant",
            response.Name);

        Assert.Equal(
            "usuario@workflow.test",
            response.Email);

        Assert.Equal(
            UserRole.Member,
            response.Role);
    }
}