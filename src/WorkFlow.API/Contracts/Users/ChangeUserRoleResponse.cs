using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Users;

public sealed record ChangeUserRoleResponse(
    Guid PublicId,
    Guid TenantPublicId,
    UserRole Role,
    DateTime? UpdatedAt);