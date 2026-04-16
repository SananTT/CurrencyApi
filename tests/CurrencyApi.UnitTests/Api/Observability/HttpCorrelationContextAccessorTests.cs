using System.Diagnostics;
using CurrencyApi.Api.Observability;
using Microsoft.AspNetCore.Http;

namespace CurrencyApi.UnitTests.Api.Observability;

public sealed class HttpCorrelationContextAccessorTests
{
    [Fact]
    public void GetCorrelationId_ShouldPreferHttpContextTraceIdentifier()
    {
        var accessor = new HttpCorrelationContextAccessor(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "corr-http",
            },
        });

        var result = accessor.GetCorrelationId();

        Assert.Equal("corr-http", result);
    }

    [Fact]
    public void GetCorrelationId_ShouldFallbackToActivityTraceId_WhenHttpContextIsMissing()
    {
        using var activity = new Activity("test");
        activity.Start();

        var accessor = new HttpCorrelationContextAccessor(new HttpContextAccessor());

        var result = accessor.GetCorrelationId();

        Assert.Equal(activity.TraceId.ToString(), result);
    }

    [Fact]
    public void GetCorrelationId_ShouldGenerateNewValue_WhenNoContextExists()
    {
        var accessor = new HttpCorrelationContextAccessor(new HttpContextAccessor());

        var result = accessor.GetCorrelationId();

        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Equal(32, result.Length);
    }
}
