using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CurrencyApi.Application.Auth.Contracts;
using Microsoft.IdentityModel.Tokens;

namespace CurrencyApi.Api.Authentication;

public sealed class JwtAccessTokenIssuer : IAccessTokenIssuer
{
    private readonly JwtAuthSettings _settings;

    public JwtAccessTokenIssuer(JwtAuthSettings settings)
    {
        _settings = settings;
    }

    public AccessTokenResult IssueToken(AuthenticatedUser user)
    {
        var issuedAt = DateTimeOffset.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_settings.ExpiryMinutes);
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SigningKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("client_id", user.ClientId),
            new Claim(ClaimTypes.Role, user.Role),
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new AccessTokenResult(
            AccessToken: new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc: expiresAt);
    }
}
