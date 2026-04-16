using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CurrencyApi.Application.Admin.Contracts;
using CurrencyApi.Application.Auth.Contracts;

namespace CurrencyApi.IntegrationTests.Api;

public sealed class AdminAuthorizationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AdminAuthorizationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ClearCache_ShouldReturnUnauthorized_WhenRequestIsAnonymous()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/admin/cache/clear", new ClearCacheRequest());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ClearCache_ShouldReturnForbidden_WhenTokenBelongsToUserRole()
    {
        var token = await LoginAsync("viewer", "viewer-pass");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/cache/clear")
        {
            Content = JsonContent.Create(new ClearCacheRequest()),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ClearCache_ShouldReturnOk_WhenTokenBelongsToAdminRole()
    {
        var token = await LoginAsync("admin", "admin-pass");
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/admin/cache/clear")
        {
            Content = JsonContent.Create(new ClearCacheRequest()),
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<ClearCacheResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(0, payload!.InvalidatedEntries);
    }

    private async Task<string> LoginAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new LoginRequest(username, password));

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(payload);

        return payload!.AccessToken;
    }
}
