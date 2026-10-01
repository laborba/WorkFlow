namespace WorkFlow.Application.ProjectTasks.PauseProjectTask;

public sealed record PauseProjectTaskCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid TaskPublicId,
    Guid RequestedByUserPublicId,
    string Reason);