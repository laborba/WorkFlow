using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.ProjectTasks;

public sealed record CreateProjectTaskRequest(
    string Title,
    string? Description,
    ProjectTaskPriority Priority,
    DateTime? DueDate);
