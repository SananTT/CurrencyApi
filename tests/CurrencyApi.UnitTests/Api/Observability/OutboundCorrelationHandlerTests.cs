using CurrencyApi.Api.Observability;
using Microsoft.AspNetCore.Http;

namespace CurrencyApi.UnitTests.Api.Observability;

public sealed class OutboundCorrelationHandlerTests
{
    [Fact]
    public async Task SendAsync_ShouldAppendCorrelationHeader_FromCurrentHttpContext()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                TraceIdentifier = "corr-456",
            },
        };

        var handler = new OutboundCorrelationHandler(new HttpCorrelationContextAccessor(httpContextAccessor))
        {
            InnerHandler = new CapturingHandler(),
        };

        using var invoker = new HttpMessageInvoker(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://example.test/rates");

        await invoker.SendAsync(request, CancellationToken.None);

        Assert.True(request.Headers.TryGetValues(CorrelationHeaders.CorrelationId, out var values));
        Assert.Equal("corr-456", values.Single());
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
    }
}
