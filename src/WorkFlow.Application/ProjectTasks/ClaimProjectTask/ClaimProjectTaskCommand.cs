namespace WorkFlow.Application.ProjectTasks.ClaimProjectTask;

public sealed record ClaimProjectTaskCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid TaskPublicId,
    Guid ClaimedByUserPublicId);