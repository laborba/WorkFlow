namespace WorkFlow.Application.Users.ListUsers;

public sealed record ListUsersResult(
    IReadOnlyCollection<UserListItemResult> Items,
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages);