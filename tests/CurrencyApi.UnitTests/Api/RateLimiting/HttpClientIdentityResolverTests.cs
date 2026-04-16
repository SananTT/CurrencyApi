using System.Security.Claims;
using CurrencyApi.Api.RateLimiting;
using Microsoft.AspNetCore.Http;

namespace CurrencyApi.UnitTests.Api.RateLimiting;

public sealed class HttpClientIdentityResolverTests
{
    private readonly HttpClientIdentityResolver _resolver = new();

    [Fact]
    public void ResolveLoginPartition_ShouldUseRemoteIpAddress_WhenAvailable()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

        var result = _resolver.ResolveLoginPartition(httpContext);

        Assert.Equal("ip:127.0.0.1", result);
    }

    [Fact]
    public void ResolveAuthenticatedClientPartition_ShouldPreferClientIdClaim()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("client_id", "viewer-client")],
            "Bearer"));

        var result = _resolver.ResolveAuthenticatedClientPartition(httpContext);

        Assert.Equal("client:viewer-client", result);
    }

    [Fact]
    public void ResolveAuthenticatedClientPartition_ShouldFallbackToSubject_WhenClientIdIsMissing()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "viewer")],
            "Bearer"));

        var result = _resolver.ResolveAuthenticatedClientPartition(httpContext);

        Assert.Equal("subject:viewer", result);
    }
}
