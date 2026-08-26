namespace WorkFlow.Application.Tenants.GetTenantByPublicId;

public sealed record GetTenantByPublicIdQuery(
    Guid PublicId);