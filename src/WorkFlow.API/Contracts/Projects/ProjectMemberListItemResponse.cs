using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Projects;

public sealed record ProjectMemberListItemResponse(
    Guid UserPublicId,
    string Name,
    string Email,
    UserRole Role,
    DateTime AddedAt,
    Guid AddedByUserPublicId);