using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.Projects.CreateProject;

public sealed record CreateProjectResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid CreatedByUserPublicId,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTime? DueDate,
    DateTime CreatedAt);