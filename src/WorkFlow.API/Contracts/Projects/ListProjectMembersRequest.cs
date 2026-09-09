namespace WorkFlow.API.Contracts.Projects;

public sealed record ListProjectMembersRequest
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public string? Search { get; init; }
}