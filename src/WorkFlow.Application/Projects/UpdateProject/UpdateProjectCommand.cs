namespace WorkFlow.Application.Projects.UpdateProject;

public sealed record UpdateProjectCommand(
    Guid TenantPublicId,
    Guid ProjectPublicId,
    Guid RequestedByUserPublicId,
    string Name,
    string? Description,
    DateTime? DueDate);