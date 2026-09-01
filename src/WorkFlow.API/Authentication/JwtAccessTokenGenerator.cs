using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WorkFlow.Application.Abstractions.Security;
using WorkFlow.Domain.Entities;

namespace WorkFlow.API.Authentication;

public sealed class JwtAccessTokenGenerator :
    IAccessTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtAccessTokenGenerator(
        IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string AccessToken, DateTime ExpiresAt) Generate(
        User user,
        Guid tenantPublicId)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (tenantPublicId == Guid.Empty)
        {
            throw new ArgumentException(
                "O PublicId da empresa não pode estar vazio.",
                nameof(tenantPublicId));
        }

        var now =
            DateTime.UtcNow;

        var expiresAt =
            now.AddMinutes(
                _options.ExpirationMinutes);

        var claims =
            new[]
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    user.PublicId.ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    user.Email),

                new Claim(
                    ClaimTypes.Name,
                    user.Name),

                new Claim(
                    ClaimTypes.Role,
                    user.Role.ToString()),

                new Claim(
                    JwtClaimNames.TenantPublicId,
                    tenantPublicId.ToString())
            };

        var signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _options.Key));

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: _options.Issuer,
                audience: _options.Audience,
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: credentials);

        var accessToken =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return (
            accessToken,
            expiresAt);
    }
}