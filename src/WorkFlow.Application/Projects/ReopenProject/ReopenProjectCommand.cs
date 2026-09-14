namespace WorkFlow.Application.Projects.ReopenProject;

public sealed record ReopenProjectCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    string Reason);