namespace WorkFlow.Application.Projects.CreateProject;

public sealed record CreateProjectCommand(
    Guid TenantPublicId,
    Guid CreatedByUserPublicId,
    string Name,
    string? Description,
    DateTime? DueDate);