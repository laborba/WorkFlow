using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record UpdateProjectTaskRequest(
    string Title,
    string? Description,
    ProjectTaskPriority Priority,
    DateTime? DueDate);