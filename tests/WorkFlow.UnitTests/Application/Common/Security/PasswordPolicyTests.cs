using WorkFlow.Application.Common.Security;

namespace WorkFlow.UnitTests.Application.Common.Security;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData(15)]
    [InlineData(128)]
    public void
    Validate_ShouldNotThrow_WhenLengthIsValid(
        int passwordLength)
    {
        var password =
            new string('a', passwordLength);

        var exception =
            Record.Exception(
                () => PasswordPolicy.Validate(password));

        Assert.Null(exception);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(129)]
    public void
    Validate_ShouldThrow_WhenLengthIsInvalid(
        int passwordLength)
    {
        var password =
            new string('a', passwordLength);

        Assert.Throws<ArgumentException>(
            () => PasswordPolicy.Validate(password));
    }

    [Fact]
    public void
    Validate_ShouldCountUnicodeCodePoints()
    {
        var password =
            string.Concat(
                Enumerable.Repeat(
                    "🐱",
                    PasswordPolicy.MinimumLength));

        var exception =
            Record.Exception(
                () => PasswordPolicy.Validate(password));

        Assert.Null(exception);
    }
}