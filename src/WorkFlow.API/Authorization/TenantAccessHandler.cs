using Microsoft.AspNetCore.Authorization;
using WorkFlow.API.Authentication;

namespace WorkFlow.API.Authorization;

public sealed class TenantAccessHandler :
    AuthorizationHandler<TenantAccessRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantAccessHandler(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor =
            httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TenantAccessRequirement requirement)
    {
        var httpContext =
            _httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            return Task.CompletedTask;
        }

        var tenantClaimValue =
            context.User.FindFirst(
                JwtClaimNames.TenantPublicId)?.Value;

        if (!Guid.TryParse(
                tenantClaimValue,
                out var authenticatedTenantPublicId))
        {
            return Task.CompletedTask;
        }

        var routeTenantValue =
            httpContext.Request.RouteValues[
                "tenantPublicId"]?.ToString();

        if (!Guid.TryParse(
                routeTenantValue,
                out var routeTenantPublicId))
        {
            return Task.CompletedTask;
        }

        if (authenticatedTenantPublicId ==
            routeTenantPublicId)
        {
            context.Succeed(
                requirement);
        }

        return Task.CompletedTask;
    }
}