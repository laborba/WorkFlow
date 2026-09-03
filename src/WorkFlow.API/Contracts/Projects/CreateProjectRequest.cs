namespace WorkFlow.API.Contracts.Projects;

public sealed record CreateProjectRequest(
    string Name,
    string? Description,
    DateTime? DueDate);