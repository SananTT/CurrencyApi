namespace CurrencyApi.Api.RateLimiting;

public interface IClientIdentityResolver
{
    string ResolveLoginPartition(HttpContext httpContext);

    string ResolveAuthenticatedClientPartition(HttpContext httpContext);
}
