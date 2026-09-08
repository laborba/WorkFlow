using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.GetProjectByPublicId;

public sealed record GetProjectByPublicIdResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid CreatedByUserPublicId,
    Guid? ResponsibleUserPublicId,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ArchivedAt);