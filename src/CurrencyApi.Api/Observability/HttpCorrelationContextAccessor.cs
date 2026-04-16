using System.Diagnostics;

namespace CurrencyApi.Api.Observability;

public sealed class HttpCorrelationContextAccessor : ICorrelationContextAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCorrelationContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (!string.IsNullOrWhiteSpace(httpContext?.TraceIdentifier))
        {
            return httpContext.TraceIdentifier;
        }

        var activityTraceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(activityTraceId))
        {
            return activityTraceId;
        }

        return Guid.NewGuid().ToString("N");
    }
}
