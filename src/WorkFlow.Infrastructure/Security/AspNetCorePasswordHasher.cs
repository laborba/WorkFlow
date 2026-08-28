using Microsoft.AspNetCore.Identity;
using WorkFlow.Application.Abstractions.Security;

namespace WorkFlow.Infrastructure.Security;

public sealed class AspNetCorePasswordHasher : IPasswordHasher
{
    private static readonly object UserContext =
        new();

    private readonly PasswordHasher<object> _passwordHasher =
        new();

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "A senha não pode estar vazia.",
                nameof(password));
        }

        return _passwordHasher.HashPassword(
            UserContext,
            password);
    }

    public bool Verify(
        string password,
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "A senha não pode estar vazia.",
                nameof(password));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException(
                "O hash da senha não pode estar vazio.",
                nameof(passwordHash));
        }

        var result =
            _passwordHasher.VerifyHashedPassword(
                UserContext,
                passwordHash,
                password);

        return result is
            PasswordVerificationResult.Success or
            PasswordVerificationResult.SuccessRehashNeeded;
    }
}