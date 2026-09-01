using Microsoft.AspNetCore.Authorization;

namespace WorkFlow.API.Authorization;

public sealed class TenantAccessRequirement :
    IAuthorizationRequirement
{
}