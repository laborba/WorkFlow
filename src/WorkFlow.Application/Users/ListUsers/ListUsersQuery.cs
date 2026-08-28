using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Users.ListUsers;

public sealed record ListUsersQuery(
    Guid TenantPublicId,
    int PageNumber = 1,
    int PageSize = 20,
    UserRole? Role = null,
    bool? IsActive = null,
    string? Search = null);