using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using WorkFlow.API.Authentication;
using WorkFlow.API.Authorization;

namespace WorkFlow.UnitTests.API.Authorization;

public sealed class TenantAccessHandlerTests
{
    [Fact]
    public async Task
    HandleAsync_ShouldSucceed_WhenAuthenticatedTenantMatchesRouteTenant()
    {
        var tenantPublicId =
            Guid.NewGuid();

        var claims =
            new[]
            {
                new Claim(
                    JwtClaimNames.TenantPublicId,
                    tenantPublicId.ToString())
            };

        var identity =
            new ClaimsIdentity(
                claims,
                "Test");

        var user =
            new ClaimsPrincipal(
                identity);

        var httpContext =
            new DefaultHttpContext();

        httpContext.Request.RouteValues[
            "tenantPublicId"] =
            tenantPublicId.ToString();

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext = httpContext
            };

        var handler =
            new TenantAccessHandler(
                httpContextAccessor);

        var requirement =
            new TenantAccessRequirement();

        var authorizationContext =
            new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                resource: null);

        await handler.HandleAsync(
            authorizationContext);

        Assert.True(
            authorizationContext.HasSucceeded);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldFail_WhenAuthenticatedTenantDoesNotMatchRouteTenant()
    {
        var authenticatedTenantPublicId =
            Guid.NewGuid();

        var routeTenantPublicId =
            Guid.NewGuid();

        var claims =
            new[]
            {
            new Claim(
                JwtClaimNames.TenantPublicId,
                authenticatedTenantPublicId.ToString())
            };

        var identity =
            new ClaimsIdentity(
                claims,
                "Test");

        var user =
            new ClaimsPrincipal(
                identity);

        var httpContext =
            new DefaultHttpContext();

        httpContext.Request.RouteValues[
            "tenantPublicId"] =
            routeTenantPublicId.ToString();

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext = httpContext
            };

        var handler =
            new TenantAccessHandler(
                httpContextAccessor);

        var requirement =
            new TenantAccessRequirement();

        var authorizationContext =
            new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                resource: null);

        await handler.HandleAsync(
            authorizationContext);

        Assert.False(
            authorizationContext.HasSucceeded);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldFail_WhenTenantClaimDoesNotExist()
    {
        var routeTenantPublicId =
            Guid.NewGuid();

        var identity =
            new ClaimsIdentity(
                authenticationType: "Test");

        var user =
            new ClaimsPrincipal(
                identity);

        var httpContext =
            new DefaultHttpContext();

        httpContext.Request.RouteValues[
            "tenantPublicId"] =
            routeTenantPublicId.ToString();

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext = httpContext
            };

        var handler =
            new TenantAccessHandler(
                httpContextAccessor);

        var requirement =
            new TenantAccessRequirement();

        var authorizationContext =
            new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                resource: null);

        await handler.HandleAsync(
            authorizationContext);

        Assert.False(
            authorizationContext.HasSucceeded);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldFail_WhenTenantClaimIsInvalid()
    {
        var routeTenantPublicId =
            Guid.NewGuid();

        var claims =
            new[]
            {
            new Claim(
                JwtClaimNames.TenantPublicId,
                "invalid-guid")
            };

        var identity =
            new ClaimsIdentity(
                claims,
                "Test");

        var user =
            new ClaimsPrincipal(
                identity);

        var httpContext =
            new DefaultHttpContext();

        httpContext.Request.RouteValues[
            "tenantPublicId"] =
            routeTenantPublicId.ToString();

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext = httpContext
            };

        var handler =
            new TenantAccessHandler(
                httpContextAccessor);

        var requirement =
            new TenantAccessRequirement();

        var authorizationContext =
            new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                resource: null);

        await handler.HandleAsync(
            authorizationContext);

        Assert.False(
            authorizationContext.HasSucceeded);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldFail_WhenRouteTenantPublicIdDoesNotExist()
    {
        var authenticatedTenantPublicId =
            Guid.NewGuid();

        var claims =
            new[]
            {
            new Claim(
                JwtClaimNames.TenantPublicId,
                authenticatedTenantPublicId.ToString())
            };

        var identity =
            new ClaimsIdentity(
                claims,
                "Test");

        var user =
            new ClaimsPrincipal(
                identity);

        var httpContext =
            new DefaultHttpContext();

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext = httpContext
            };

        var handler =
            new TenantAccessHandler(
                httpContextAccessor);

        var requirement =
            new TenantAccessRequirement();

        var authorizationContext =
            new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                resource: null);

        await handler.HandleAsync(
            authorizationContext);

        Assert.False(
            authorizationContext.HasSucceeded);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldFail_WhenRouteTenantPublicIdIsInvalid()
    {
        var authenticatedTenantPublicId =
            Guid.NewGuid();

        var claims =
            new[]
            {
            new Claim(
                JwtClaimNames.TenantPublicId,
                authenticatedTenantPublicId.ToString())
            };

        var identity =
            new ClaimsIdentity(
                claims,
                "Test");

        var user =
            new ClaimsPrincipal(
                identity);

        var httpContext =
            new DefaultHttpContext();

        httpContext.Request.RouteValues[
            "tenantPublicId"] =
            "invalid-guid";

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext = httpContext
            };

        var handler =
            new TenantAccessHandler(
                httpContextAccessor);

        var requirement =
            new TenantAccessRequirement();

        var authorizationContext =
            new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                resource: null);

        await handler.HandleAsync(
            authorizationContext);

        Assert.False(
            authorizationContext.HasSucceeded);
    }

    [Fact]
    public async Task
    HandleAsync_ShouldFail_WhenHttpContextDoesNotExist()
    {
        var tenantPublicId =
            Guid.NewGuid();

        var claims =
            new[]
            {
            new Claim(
                JwtClaimNames.TenantPublicId,
                tenantPublicId.ToString())
            };

        var identity =
            new ClaimsIdentity(
                claims,
                "Test");

        var user =
            new ClaimsPrincipal(
                identity);

        var httpContextAccessor =
            new HttpContextAccessor
            {
                HttpContext = null
            };

        var handler =
            new TenantAccessHandler(
                httpContextAccessor);

        var requirement =
            new TenantAccessRequirement();

        var authorizationContext =
            new AuthorizationHandlerContext(
                new[] { requirement },
                user,
                resource: null);

        await handler.HandleAsync(
            authorizationContext);

        Assert.False(
            authorizationContext.HasSucceeded);
    }
}