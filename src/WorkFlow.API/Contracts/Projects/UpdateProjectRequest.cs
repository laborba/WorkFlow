namespace WorkFlow.API.Contracts.Projects;

public sealed record UpdateProjectRequest(
    string Name,
    string? Description,
    DateTime? DueDate);