using WorkFlow.Domain.Enums;

namespace WorkFlow.API.Contracts.Users;

public sealed record CreateUserRequest(
    string Name,
    string Email,
    string Password,
    UserRole Role);