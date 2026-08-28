using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Users.CreateUser;

public sealed record CreateUserResult(
    Guid PublicId,
    Guid TenantPublicId,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt);