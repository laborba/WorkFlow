namespace WorkFlow.Application.Users.ChangeUserStatus;

public sealed record ChangeUserStatusCommand(
    Guid TenantPublicId,
    Guid UserPublicId,
    bool IsActive);