using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Abstractions.Persistence.Models;

public sealed record ProjectMemberPermissionListItemData(
    ProjectPermission Permission,
    Guid GrantedByUserPublicId,
    DateTime GrantedAt);