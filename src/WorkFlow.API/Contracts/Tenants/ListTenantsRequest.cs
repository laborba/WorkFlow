namespace WorkFlow.API.Contracts.Tenants;

public sealed record ListTenantsRequest
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public bool? IsActive { get; init; }

    public string? Search { get; init; }
}