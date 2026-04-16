using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CurrencyApi.Application.Auth.Contracts;
using CurrencyApi.Application.Common.Contracts;
using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Application.Rates.UseCases.GetHistoricalRates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CurrencyApi.IntegrationTests.Api;

public sealed class RateLimitingTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public RateLimitingTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ShouldReturnTooManyRequests_WhenPermitLimitIsExceeded()
    {
        using var client = CreateClient();

        HttpResponseMessage? rejectedResponse = null;

        for (var attempt = 0; attempt < 6; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("viewer", "viewer-pass"));

            if (attempt < 5)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            else
            {
                rejectedResponse = response;
            }
        }

        Assert.NotNull(rejectedResponse);
        var payload = await rejectedResponse!.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("rate_limit.exceeded", payload!.Code);
        Assert.False(string.IsNullOrWhiteSpace(payload.TraceId));
    }

    [Fact]
    public async Task Historical_ShouldReturnTooManyRequests_WhenPermitLimitIsExceededForSameClient()
    {
        using var client = CreateClient(
            services =>
            {
                services.RemoveAll<IGetHistoricalRatesUseCase>();
                services.AddSingleton<IGetHistoricalRatesUseCase, StubHistoricalRatesUseCase>();
            });

        var token = await LoginAsync(client, "viewer", "viewer-pass");

        HttpResponseMessage? rejectedResponse = null;

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var response = await SendHistoricalAsync(client, token);

            if (attempt < 3)
            {
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            else
            {
                rejectedResponse = response;
            }
        }

        Assert.NotNull(rejectedResponse);
        var payload = await rejectedResponse!.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal("rate_limit.exceeded", payload!.Code);
    }

    private HttpClient CreateClient(
        Action<IServiceCollection>? configureServices = null)
    {
        return CreateClient(new Dictionary<string, string?>(), configureServices);
    }

    private HttpClient CreateClient(
        IReadOnlyDictionary<string, string?> overrides,
        Action<IServiceCollection>? configureServices = null)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(overrides);
            });

            if (configureServices is not null)
            {
                builder.ConfigureServices(configureServices);
            }
        }).CreateClient();
    }

    private static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(username, password));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(payload);

        return payload!.AccessToken;
    }

    private static async Task<HttpResponseMessage> SendHistoricalAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/rates/historical?base=EUR&start=2024-01-01&end=2024-01-02&page=1&pageSize=10");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.SendAsync(request);
    }

    private sealed class StubHistoricalRatesUseCase : IGetHistoricalRatesUseCase
    {
        public Task<HistoricalRatesResponse> ExecuteAsync(
            HistoricalRatesRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HistoricalRatesResponse(
                BaseCurrency: request.BaseCurrency,
                StartDate: request.StartDate,
                EndDate: request.EndDate,
                Sort: "date_desc",
                Page: new PagedResponse<HistoricalRateItemResponse>(
                    Page: request.Page,
                    PageSize: request.PageSize,
                    TotalItems: 1,
                    TotalPages: 1,
                    Items:
                    [
                        new HistoricalRateItemResponse(
                            request.EndDate,
                            new Dictionary<string, decimal>
                            {
                                ["USD"] = 1.10m,
                            }),
                    ])));
        }
    }
}
