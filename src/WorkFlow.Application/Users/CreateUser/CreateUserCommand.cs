using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Users.CreateUser;

public sealed record CreateUserCommand(
    Guid TenantPublicId,
    string Name,
    string Email,
    string Password,
    UserRole Role);