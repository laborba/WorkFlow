using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Users.ListUsers;

public sealed record UserListItemResult(
    Guid PublicId,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);