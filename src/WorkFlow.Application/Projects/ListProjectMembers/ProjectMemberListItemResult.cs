using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.ListProjectMembers;

public sealed record ProjectMemberListItemResult(
    Guid UserPublicId,
    string Name,
    string Email,
    UserRole Role,
    DateTime AddedAt,
    Guid AddedByUserPublicId);