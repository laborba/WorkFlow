namespace WorkFlow.API.Contracts.Users;

public sealed record ChangeUserStatusRequest(
    bool IsActive);