namespace WorkFlow.Application.Projects.ResumeProject;

public sealed record ResumeProjectCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    DateTime? NewDueDate);