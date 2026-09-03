using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Projects;

public sealed record CreateProjectResponse(
    Guid PublicId,
    Guid TenantPublicId,
    Guid CreatedByUserPublicId,
    string Name,
    string? Description,
    ProjectStatus Status,
    DateTime? DueDate,
    DateTime CreatedAt);