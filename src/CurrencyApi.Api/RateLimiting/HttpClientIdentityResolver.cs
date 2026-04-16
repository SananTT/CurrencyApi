namespace CurrencyApi.Api.RateLimiting;

public sealed class HttpClientIdentityResolver : IClientIdentityResolver
{
    public string ResolveLoginPartition(HttpContext httpContext)
    {
        var remoteIpAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(remoteIpAddress))
        {
            return $"ip:{remoteIpAddress}";
        }

        return "ip:unknown";
    }

    public string ResolveAuthenticatedClientPartition(HttpContext httpContext)
    {
        var clientId = httpContext.User.FindFirst("client_id")?.Value;
        if (!string.IsNullOrWhiteSpace(clientId))
        {
            return $"client:{clientId}";
        }

        var subject = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext.User.FindFirst("sub")?.Value
            ?? httpContext.User.Identity?.Name;

        if (!string.IsNullOrWhiteSpace(subject))
        {
            return $"subject:{subject}";
        }

        return ResolveLoginPartition(httpContext);
    }
}
