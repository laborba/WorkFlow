using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Users;

public sealed record GetUserByPublicIdResponse(
    Guid PublicId,
    Guid TenantPublicId,
    string Name,
    string Email,
    UserRole Role,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);