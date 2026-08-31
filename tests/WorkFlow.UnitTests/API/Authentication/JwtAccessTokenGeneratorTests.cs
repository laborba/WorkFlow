using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WorkFlow.API.Authentication;
using WorkFlow.Domain.Entities;
using WorkFlow.Domain.Enums;

namespace WorkFlow.UnitTests.API.Authentication;

public sealed class JwtAccessTokenGeneratorTests
{
    [Fact]
    public void
    Generate_ShouldCreateTokenWithExpectedClaims()
    {
        var options =
            Options.Create(
                new JwtOptions
                {
                    Issuer = "WorkFlow.API.Tests",
                    Audience = "WorkFlow.Client.Tests",
                    Key =
                        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_=!",
                    ExpirationMinutes = 60
                });

        var generator =
            new JwtAccessTokenGenerator(
                options);

        var user =
            new User(
                42,
                "Usuário JWT",
                "jwt@test.local",
                "password-hash-for-test",
                UserRole.ProjectManager);

        var tenantPublicId =
            Guid.NewGuid();

        var beforeGeneration =
            DateTime.UtcNow;

        var result =
            generator.Generate(
                user,
                tenantPublicId);

        var afterGeneration =
            DateTime.UtcNow;

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));

        Assert.InRange(
            result.ExpiresAt,
            beforeGeneration.AddMinutes(60),
            afterGeneration.AddMinutes(60));

        var token =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(
                    result.AccessToken);

        Assert.Equal(
            "WorkFlow.API.Tests",
            token.Issuer);

        Assert.Contains(
            "WorkFlow.Client.Tests",
            token.Audiences);

        Assert.Equal(
            user.PublicId.ToString(),
            token.Claims
                .Single(
                    claim =>
                        claim.Type ==
                        JwtRegisteredClaimNames.Sub)
                .Value);

        Assert.Equal(
            user.Email,
            token.Claims
                .Single(
                    claim =>
                        claim.Type ==
                        JwtRegisteredClaimNames.Email)
                .Value);

        Assert.Equal(
            user.Name,
            token.Claims
                .Single(
                    claim =>
                        claim.Type ==
                        ClaimTypes.Name)
                .Value);

        Assert.Equal(
            user.Role.ToString(),
            token.Claims
                .Single(
                    claim =>
                        claim.Type ==
                        ClaimTypes.Role)
                .Value);

        Assert.Equal(
            tenantPublicId.ToString(),
            token.Claims
                .Single(
                    claim =>
                        claim.Type ==
                        "tenant_public_id")
                .Value);
    }

    [Fact]
    public void
    Generate_ShouldThrow_WhenTenantPublicIdIsEmpty()
    {
        var options =
            Options.Create(
                new JwtOptions
                {
                    Issuer = "WorkFlow.API.Tests",
                    Audience = "WorkFlow.Client.Tests",
                    Key =
                        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_=!",
                    ExpirationMinutes = 60
                });

        var generator =
            new JwtAccessTokenGenerator(
                options);

        var user =
            new User(
                42,
                "Usuário JWT",
                "jwt@test.local",
                "password-hash-for-test",
                UserRole.Member);

        var exception =
            Assert.Throws<ArgumentException>(
                () => generator.Generate(
                    user,
                    Guid.Empty));

        Assert.Equal(
            "tenantPublicId",
            exception.ParamName);
    }

    [Fact]
    public void
    Generate_ShouldThrow_WhenUserIsNull()
    {
        var options =
            Options.Create(
                new JwtOptions
                {
                    Issuer = "WorkFlow.API.Tests",
                    Audience = "WorkFlow.Client.Tests",
                    Key =
                        "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_=!",
                    ExpirationMinutes = 60
                });

        var generator =
            new JwtAccessTokenGenerator(
                options);

        var tenantPublicId =
            Guid.NewGuid();

        Assert.Throws<ArgumentNullException>(
            () => generator.Generate(
                null!,
                tenantPublicId));
    }

    [Fact]
    public void
    Generate_ShouldCreateTokenThatPassesValidation()
    {
        const string issuer =
            "WorkFlow.API.Tests";

        const string audience =
            "WorkFlow.Client.Tests";

        const string key =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_=!";

        var options =
            Options.Create(
                new JwtOptions
                {
                    Issuer = issuer,
                    Audience = audience,
                    Key = key,
                    ExpirationMinutes = 60
                });

        var generator =
            new JwtAccessTokenGenerator(
                options);

        var user =
            new User(
                42,
                "Usuário JWT",
                "jwt@test.local",
                "password-hash-for-test",
                UserRole.Member);

        var tenantPublicId =
            Guid.NewGuid();

        var result =
            generator.Generate(
                user,
                tenantPublicId);

        var validationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            key)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };

        var handler =
            new JwtSecurityTokenHandler();

        var principal =
            handler.ValidateToken(
                result.AccessToken,
                validationParameters,
                out var validatedToken);

        Assert.NotNull(
            principal);

        Assert.IsType<JwtSecurityToken>(
            validatedToken);
    }

    [Fact]
    public void
    Generate_ShouldCreateTokenThatFailsValidationWithDifferentKey()
    {
        const string issuer =
            "WorkFlow.API.Tests";

        const string audience =
            "WorkFlow.Client.Tests";

        const string signingKey =
            "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz-_=!";

        const string differentKey =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz-_=!";

        var options =
            Options.Create(
                new JwtOptions
                {
                    Issuer = issuer,
                    Audience = audience,
                    Key = signingKey,
                    ExpirationMinutes = 60
                });

        var generator =
            new JwtAccessTokenGenerator(
                options);

        var user =
            new User(
                42,
                "Usuário JWT",
                "jwt@test.local",
                "password-hash-for-test",
                UserRole.Member);

        var result =
            generator.Generate(
                user,
                Guid.NewGuid());

        var validationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = issuer,

                ValidateAudience = true,
                ValidAudience = audience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            differentKey)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.Zero
            };

        var handler =
            new JwtSecurityTokenHandler();

        Assert.ThrowsAny<SecurityTokenException>(
            () => handler.ValidateToken(
                result.AccessToken,
                validationParameters,
                out _));
    }
}