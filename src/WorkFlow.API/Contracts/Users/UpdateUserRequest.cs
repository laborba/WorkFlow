namespace WorkFlow.API.Contracts.Users;

public sealed record UpdateUserRequest(
    string Name,
    string Email);