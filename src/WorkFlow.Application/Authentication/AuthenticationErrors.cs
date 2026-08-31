using WorkFlow.Application.Common.Errors;

namespace WorkFlow.Application.Authentication;

public static class AuthenticationErrors
{
    public static Error InvalidCredentials { get; } =
        new(
            "Authentication.InvalidCredentials",
            "E-mail ou senha inválidos.");

    public static Error UserInactive { get; } =
        new(
            "Authentication.UserInactive",
            "O usuário está desativado.");
}