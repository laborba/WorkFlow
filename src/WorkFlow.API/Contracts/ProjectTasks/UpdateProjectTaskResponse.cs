using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record UpdateProjectTaskResponse(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    string Title,
    string? Description,
    ProjectTaskStatus Status,
    ProjectTaskPriority Priority,
    DateTime? DueDate,
    DateTime? UpdatedAt);