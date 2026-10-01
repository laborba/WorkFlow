namespace WorkFlow.Application.ProjectTasks.ResumeProjectTask;

public sealed record ResumeProjectTaskCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid TaskPublicId,
    Guid RequestedByUserPublicId,
    DateTime? NewDueDate);