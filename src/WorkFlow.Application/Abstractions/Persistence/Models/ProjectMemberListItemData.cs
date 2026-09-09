using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Abstractions.Persistence.Models;

public sealed record ProjectMemberListItemData(
    Guid UserPublicId,
    string Name,
    string Email,
    UserRole Role,
    DateTime AddedAt,
    Guid AddedByUserPublicId);