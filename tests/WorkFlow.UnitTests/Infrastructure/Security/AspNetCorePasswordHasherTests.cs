using WorkFlow.Infrastructure.Security;

namespace WorkFlow.UnitTests.Infrastructure.Security;

public class AspNetCorePasswordHasherTests
{
    private const string ValidPassword =
        "uma senha longa e segura";

    [Fact]
    public void
    Hash_ShouldReturnHashDifferentFromOriginalPassword()
    {
        var passwordHasher =
            new AspNetCorePasswordHasher();

        var passwordHash =
            passwordHasher.Hash(
                ValidPassword);

        Assert.False(
            string.IsNullOrWhiteSpace(passwordHash));

        Assert.NotEqual(
            ValidPassword,
            passwordHash);
    }

    [Fact]
    public void
    Hash_ShouldReturnDifferentHashes_ForSamePassword()
    {
        var passwordHasher =
            new AspNetCorePasswordHasher();

        var firstHash =
            passwordHasher.Hash(
                ValidPassword);

        var secondHash =
            passwordHasher.Hash(
                ValidPassword);

        Assert.NotEqual(
            firstHash,
            secondHash);
    }

    [Fact]
    public void
    Verify_ShouldReturnTrue_WhenPasswordIsCorrect()
    {
        var passwordHasher =
            new AspNetCorePasswordHasher();

        var passwordHash =
            passwordHasher.Hash(
                ValidPassword);

        var result =
            passwordHasher.Verify(
                ValidPassword,
                passwordHash);

        Assert.True(result);
    }

    [Fact]
    public void
    Verify_ShouldReturnFalse_WhenPasswordIsIncorrect()
    {
        var passwordHasher =
            new AspNetCorePasswordHasher();

        var passwordHash =
            passwordHasher.Hash(
                ValidPassword);

        var result =
            passwordHasher.Verify(
                "outra senha longa e segura",
                passwordHash);

        Assert.False(result);
    }
}