using WorkFlow.Domain.Common;

namespace WorkFlow.UnitTests.Domain.Common;

public sealed class EmailNormalizerTests
{
    [Fact]
    public void Normalize_ShouldTrimAndConvertToUpperInvariant()
    {
        var normalizedEmail =
            EmailNormalizer.Normalize(
                "  Lucas.Teste@Empresa.COM  ");

        Assert.Equal(
            "LUCAS.TESTE@EMPRESA.COM",
            normalizedEmail);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_ShouldThrow_WhenEmailIsInvalid(
        string? invalidEmail)
    {
        Assert.Throws<ArgumentException>(() =>
            EmailNormalizer.Normalize(
                invalidEmail!));
    }
}