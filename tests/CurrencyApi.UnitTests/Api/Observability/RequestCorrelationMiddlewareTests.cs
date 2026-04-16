using System.Text;
using CurrencyApi.Api.Observability;
using Microsoft.AspNetCore.Http;

namespace CurrencyApi.UnitTests.Api.Observability;

public sealed class RequestCorrelationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldUseIncomingCorrelationId_WhenHeaderExists()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[CorrelationHeaders.CorrelationId] = "corr-123";
        context.Response.Body = new MemoryStream();

        var middleware = new RequestCorrelationMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("corr-123", context.TraceIdentifier);
        Assert.Equal("corr-123", context.Response.Headers[CorrelationHeaders.CorrelationId]);
    }

    [Fact]
    public async Task InvokeAsync_ShouldGenerateCorrelationId_WhenHeaderIsMissing()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new RequestCorrelationMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.False(string.IsNullOrWhiteSpace(context.TraceIdentifier));
        Assert.Equal(context.TraceIdentifier, context.Response.Headers[CorrelationHeaders.CorrelationId]);
    }
}
