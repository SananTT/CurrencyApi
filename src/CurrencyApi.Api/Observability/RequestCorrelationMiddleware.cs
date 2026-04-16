using Serilog.Context;

namespace CurrencyApi.Api.Observability;

public sealed class RequestCorrelationMiddleware
{
    private readonly RequestDelegate _next;

    public RequestCorrelationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var correlationId = ResolveCorrelationId(httpContext);
        httpContext.TraceIdentifier = correlationId;
        httpContext.Response.Headers[CorrelationHeaders.CorrelationId] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", correlationId))
        {
            await _next(httpContext);
        }
    }

    private static string ResolveCorrelationId(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(CorrelationHeaders.CorrelationId, out var values))
        {
            var providedCorrelationId = values.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(providedCorrelationId))
            {
                return providedCorrelationId;
            }
        }

        return Guid.NewGuid().ToString("N");
    }
}
