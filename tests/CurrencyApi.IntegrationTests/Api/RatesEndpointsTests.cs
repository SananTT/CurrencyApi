using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CurrencyApi.Application.Auth.Contracts;
using CurrencyApi.Application.Common.Contracts;
using CurrencyApi.IntegrationTests.Api.Fakes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;

namespace CurrencyApi.IntegrationTests.Api;

public sealed class RatesEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public RatesEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetLatest_WithoutToken_ShouldReturnUnauthorized()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/api/v1/rates/latest?base=EUR");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetLatest_WithToken_ShouldReturnOk_AndCorrectData()
    {
        using var client = CreateClient();
        var token = await LoginAsync(client, "viewer", "viewer-pass");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rates/latest?base=EUR");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("EUR", json);
        Assert.Contains("1.1", json); // Fake USD rate
    }

    [Fact]
    public async Task Convert_WithToken_ShouldReturnOk_AndCalculatedValue()
    {
        using var client = CreateClient();
        var token = await LoginAsync(client, "viewer", "viewer-pass");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rates/convert?from=EUR&to=USD&amount=100");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("110", json); // 100 * 1.1 = 110
    }

    [Fact]
    public async Task GetHistorical_WithToken_ShouldReturnOk_AndPagedData()
    {
        using var client = CreateClient();
        var token = await LoginAsync(client, "viewer", "viewer-pass");

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/rates/historical?base=EUR&start=2024-01-01&end=2024-01-05&page=1&pageSize=10");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.SendAsync(request);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("2024-01-01", json);
    }

    private HttpClient CreateClient()
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.Configure<HttpClientFactoryOptions>("IFrankfurterApiClient", options =>
                {
                    options.HttpMessageHandlerBuilderActions.Add(b =>
                    {
                        b.PrimaryHandler = new FakeFrankfurterMessageHandler(HttpStatusCode.OK);
                    });
                });
            });
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
}
