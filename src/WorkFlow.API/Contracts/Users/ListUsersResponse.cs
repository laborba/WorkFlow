namespace WorkFlow.API.Contracts.Users;

public sealed record ListUsersResponse(
    IReadOnlyCollection<UserListItemResponse> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);