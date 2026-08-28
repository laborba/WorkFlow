using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Users;

public sealed record UserListItemResponse(
    Guid PublicId,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);