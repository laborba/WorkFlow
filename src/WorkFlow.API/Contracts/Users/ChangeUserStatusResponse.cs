namespace WorkFlow.API.Contracts.Users;

public sealed record ChangeUserStatusResponse(
    Guid PublicId,
    Guid TenantPublicId,
    bool IsActive,
    DateTime? UpdatedAt);