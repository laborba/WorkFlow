using WorkFlow.Application.Abstractions.Security;

namespace WorkFlow.UnitTests.Application.Users.Fakes;

internal sealed class FakePasswordHasher : IPasswordHasher
{
    public string HashToReturn { get; set; } =
        "hashed-password";

    public string? PasswordReceivedForHash { get; private set; }

    public string? PasswordReceivedForVerification
    {
        get;
        private set;
    }

    public string? HashReceivedForVerification
    {
        get;
        private set;
    }

    public bool VerificationResult { get; set; }

    public string Hash(string password)
    {
        PasswordReceivedForHash =
            password;

        return HashToReturn;
    }

    public bool Verify(
        string password,
        string passwordHash)
    {
        PasswordReceivedForVerification =
            password;

        HashReceivedForVerification =
            passwordHash;

        return VerificationResult;
    }
}