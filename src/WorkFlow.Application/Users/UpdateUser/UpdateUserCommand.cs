namespace WorkFlow.Application.Users.UpdateUser;

public sealed record UpdateUserCommand(
    Guid TenantPublicId,
    Guid UserPublicId,
    string Name,
    string Email);