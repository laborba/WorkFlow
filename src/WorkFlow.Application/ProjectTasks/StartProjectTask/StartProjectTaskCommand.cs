namespace WorkFlow.Application.ProjectTasks.StartProjectTask;

public sealed record StartProjectTaskCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid TaskPublicId,
    Guid StartedByUserPublicId);