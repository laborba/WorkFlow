using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.ListProjectTasks;

public sealed record ProjectTaskListItemResult(
    Guid PublicId,
    Guid CreatedByUserPublicId,
    Guid? ResponsibleUserPublicId,
    string? ResponsibleUserName,
    Guid? ValidatorUserPublicId,
    string Title,
    string? Description,
    ProjectTaskStatus Status,
    ProjectTaskPriority Priority,
    DateTime? DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ArchivedAt);