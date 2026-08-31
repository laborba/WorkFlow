namespace WorkFlow.Application.Users.ChangeUserStatus;

public sealed record ChangeUserStatusResult(
    Guid PublicId,
    Guid TenantPublicId,
    bool IsActive,
    DateTime? UpdatedAt);