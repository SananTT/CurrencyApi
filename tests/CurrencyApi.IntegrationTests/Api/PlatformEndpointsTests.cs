using System.Net;
using System.Text.Json;
using CurrencyApi.Api.Versioning;

namespace CurrencyApi.IntegrationTests.Api;

public sealed class PlatformEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlatformEndpointsTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Versions_ShouldReturnSupportedVersions()
    {
        var response = await _client.GetAsync("/api/versions");
        using var content = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(ApiVersions.V1, document.RootElement.GetProperty("defaultVersion").GetString());
        Assert.Contains(
            document.RootElement.GetProperty("supportedVersions").EnumerateArray().Select(item => item.GetString()),
            value => value == ApiVersions.V1);
    }

    [Fact]
    public async Task SwaggerJson_ShouldExposeVersionedRoutes_AndBearerSecurity()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        using var content = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(document.RootElement.GetProperty("paths").TryGetProperty("/api/v1/rates/latest", out _));
        Assert.True(document.RootElement
            .GetProperty("components")
            .GetProperty("securitySchemes")
            .TryGetProperty("Bearer", out _));

        var latestGet = document.RootElement
            .GetProperty("paths")
            .GetProperty("/api/v1/rates/latest")
            .GetProperty("get");

        Assert.True(latestGet.TryGetProperty("security", out var security));
        Assert.True(security.GetArrayLength() > 0);
    }
}
