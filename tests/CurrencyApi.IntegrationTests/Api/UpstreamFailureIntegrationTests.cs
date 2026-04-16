using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using System.Net.Http.Headers;
using CurrencyApi.Application.Auth.Contracts;
using System.Net.Http.Json;
using CurrencyApi.IntegrationTests.Api.Fakes;

namespace CurrencyApi.IntegrationTests.Api;

public sealed class UpstreamFailureIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public UpstreamFailureIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLatest_WhenUpstreamReturns500_ShouldMapToServiceUnavailable()
    {
        using var client = CreateClient(HttpStatusCode.InternalServerError);
        var token = await LoginAsync(client, "viewer", "viewer-pass");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rates/latest?base=EUR");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("upstream", json);
    }

    [Fact]
    public async Task GetLatest_WhenUpstreamReturns404_ShouldMapToBadGateway()
    {
        // 404 is considered non-transient, so it raises FrankfurterUpstreamException, which maps to BadGateway
        using var client = CreateClient(HttpStatusCode.NotFound);
        var token = await LoginAsync(client, "viewer", "viewer-pass");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rates/latest?base=EUR");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [Fact]
    public async Task Convert_WhenUpstreamReturnsTimeout_ShouldMapToUnavailable()
    {
        using var client = CreateClient(HttpStatusCode.RequestTimeout, throwException: true);
        var token = await LoginAsync(client, "viewer", "viewer-pass");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rates/convert?from=EUR&to=USD&amount=100");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    private HttpClient CreateClient(HttpStatusCode fakeUpstreamStatus, bool throwException = false)
    {
        // Reduce cache timeout to avoid hitting caching layer and force upstream calls
        var overrides = new Dictionary<string, string?>
        {
            ["Cache:LatestTtlSeconds"] = "0"
        };

        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(overrides));
            builder.ConfigureServices(services =>
            {
                services.Configure<HttpClientFactoryOptions>("IFrankfurterApiClient", options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b =>
                    {
                        b.PrimaryHandler = new FakeFrankfurterMessageHandler(fakeUpstreamStatus, throwException);
                    });
                });
            });
        }).CreateClient();
    }

    private static async Task<string> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(username, password));
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return payload!.AccessToken;
    }
}
