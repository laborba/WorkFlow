namespace WorkFlow.Application.Common.Security;

public static class PasswordPolicy
{
    public const int MinimumLength = 15;

    public const int MaximumLength = 128;

    public static void Validate(
        string password,
        string? parameterName = null)
    {
        parameterName ??=
            nameof(password);

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "A senha não pode estar vazia.",
                parameterName);
        }

        var passwordLength =
            password.EnumerateRunes().Count();

        if (passwordLength < MinimumLength ||
            passwordLength > MaximumLength)
        {
            throw new ArgumentException(
                $"A senha deve conter entre " +
                $"{MinimumLength} e " +
                $"{MaximumLength} caracteres.",
                parameterName);
        }
    }
}