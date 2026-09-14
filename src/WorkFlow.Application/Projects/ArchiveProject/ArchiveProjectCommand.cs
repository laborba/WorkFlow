namespace WorkFlow.Application.Projects.ArchiveProject;

public sealed record ArchiveProjectCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId);