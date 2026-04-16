using System.Diagnostics;

namespace CurrencyApi.Api.Observability;

internal sealed class RequestLoggingMiddleware
{
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await _next(httpContext);
        }
        finally
        {
            stopwatch.Stop();

            var correlationId = httpContext.TraceIdentifier;
            var endpoint = httpContext.GetEndpoint()?.DisplayName ?? "unknown";
            var clientId = ResolveClaim(httpContext, "client_id");
            var username = httpContext.User.Identity?.IsAuthenticated == true
                ? httpContext.User.Identity.Name ?? ResolveClaim(httpContext, "unique_name") ?? "authenticated"
                : "anonymous";

            _logger.LogInformation(
                "HTTP request completed. Method={Method} Path={Path} StatusCode={StatusCode} ElapsedMs={ElapsedMs} Endpoint={Endpoint} CorrelationId={CorrelationId} User={User} ClientId={ClientId}",
                httpContext.Request.Method,
                httpContext.Request.Path.Value ?? "/",
                httpContext.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                endpoint,
                correlationId,
                username,
                clientId ?? "n/a");
        }
    }

    private static string? ResolveClaim(HttpContext httpContext, string claimType) =>
        httpContext.User.Claims.FirstOrDefault(claim => claim.Type == claimType)?.Value;
}
