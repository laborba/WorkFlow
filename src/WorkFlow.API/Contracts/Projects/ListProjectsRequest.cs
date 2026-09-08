namespace WorkFlow.API.Contracts.Projects;

public sealed record ListProjectsRequest
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }

    public int? Status { get; init; }

    public Guid? ResponsibleUserPublicId { get; init; }
}