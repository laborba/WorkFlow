namespace WorkFlow.Application.ProjectTasks.MoveProjectTaskToTodo;

public sealed record MoveProjectTaskToTodoCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid TaskPublicId,
    Guid RequestedByUserPublicId);