namespace WorkFlow.Application.Projects.GetProjectByPublicId;

public sealed record GetProjectByPublicIdQuery(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId);