using WorkFlow.Domain.Enums;

namespace WorkFlow.Application.ProjectTasks.MoveProjectTaskToTodo;

public sealed record MoveProjectTaskToTodoResult(
    Guid PublicId,
    Guid TenantPublicId,
    Guid ProjectPublicId,
    ProjectTaskStatus Status,
    DateTime? UpdatedAt);