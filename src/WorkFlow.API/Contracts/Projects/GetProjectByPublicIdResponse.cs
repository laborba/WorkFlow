using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Projects;

public sealed record GetProjectByPublicIdResponse(
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