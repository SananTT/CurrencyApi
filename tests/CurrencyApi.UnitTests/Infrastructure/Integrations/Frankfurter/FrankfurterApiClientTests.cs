using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Domain.Currencies;
using CurrencyApi.Infrastructure.Integrations.Frankfurter;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Configuration;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Exceptions;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace CurrencyApi.UnitTests.Infrastructure.Integrations.Frankfurter;

public sealed class FrankfurterApiClientTests
{
    [Fact]
    public async Task GetLatestRatesAsync_ShouldReturnPayload_WhenResponseIsSuccessful()
    {
        var client = CreateClient(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new FrankfurterLatestRatesResponse
                {
                    Base = "EUR",
                    Date = "2024-01-31",
                    Rates = new Dictionary<string, decimal>
                    {
                        ["USD"] = 1.08m,
                    },
                }),
            }));

        var result = await client.GetLatestRatesAsync(new LatestRatesProviderRequest(CurrencyCode.Create("EUR")));

        Assert.Equal("EUR", result.Base);
        Assert.Equal("2024-01-31", result.Date);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ShouldThrowFrankfurterUpstreamException_WhenStatusIsNonTransient()
    {
        var client = CreateClient(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)));

        var exception = await Assert.ThrowsAsync<FrankfurterUpstreamException>(() =>
            client.GetLatestRatesAsync(new LatestRatesProviderRequest(CurrencyCode.Create("EUR"))));

        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    [Fact]
    public async Task GetLatestRatesAsync_ShouldThrowHttpRequestException_WhenStatusIsTransient()
    {
        var client = CreateClient(
            new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.GetLatestRatesAsync(new LatestRatesProviderRequest(CurrencyCode.Create("EUR"))));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    private static FrankfurterApiClient CreateClient(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler)
            {
                BaseAddress = new Uri(FrankfurterClientSettings.DefaultBaseUrl),
            },
            NullLogger<FrankfurterApiClient>.Instance,
            new PassthroughResiliencePipeline(),
            new FrankfurterClientSettings());

    private sealed class PassthroughResiliencePipeline : IFrankfurterResiliencePipeline
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(_responseFactory(request));
    }
}
