using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CurrencyApi.Api.Authentication;
using CurrencyApi.Application.Auth.Contracts;

namespace CurrencyApi.UnitTests.Api.Authentication;

public sealed class JwtAccessTokenIssuerTests
{
    [Fact]
    public void IssueToken_ShouldProduceSignedJwt_WithExpectedClaims()
    {
        var issuer = new JwtAccessTokenIssuer(new JwtAuthSettings
        {
            Issuer = "CurrencyApi",
            Audience = "CurrencyApiClients",
            SigningKey = "development-signing-key-change-me-1234567890",
            ExpiryMinutes = 60,
        });

        var result = issuer.IssueToken(new AuthenticatedUser(
            UserId: "viewer-id",
            Username: "viewer",
            ClientId: "viewer-client",
            Role: SystemRoles.User));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken);

        Assert.Equal("CurrencyApi", token.Issuer);
        Assert.Contains("CurrencyApiClients", token.Audiences);
        Assert.Equal("viewer-id", token.Claims.First(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("viewer", token.Claims.First(claim => claim.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("viewer-client", token.Claims.First(claim => claim.Type == "client_id").Value);
        Assert.Equal(SystemRoles.User, token.Claims.First(claim => claim.Type == ClaimTypes.Role).Value);
        Assert.True(result.ExpiresAtUtc > DateTimeOffset.UtcNow);
    }
}
