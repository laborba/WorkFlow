namespace WorkFlow.Application.Users.GetUserByPublicId;

public sealed record GetUserByPublicIdQuery(
    Guid TenantPublicId,
    Guid UserPublicId);