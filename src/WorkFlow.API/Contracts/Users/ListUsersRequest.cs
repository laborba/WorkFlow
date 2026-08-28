namespace WorkFlow.API.Contracts.Users;

public sealed record ListUsersRequest
{
    public int PageNumber { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public int? Role { get; init; }

    public bool? IsActive { get; init; }

    public string? Search { get; init; }
}