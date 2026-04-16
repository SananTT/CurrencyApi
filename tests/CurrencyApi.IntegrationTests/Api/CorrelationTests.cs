using System.Net;
using System.Net.Http.Json;
using CurrencyApi.Api.Observability;
using CurrencyApi.Application.Common.Contracts;

namespace CurrencyApi.IntegrationTests.Api;

public sealed class CorrelationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CorrelationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task LoginValidationError_ShouldEchoCorrelationId_InHeaderAndBody()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new
            {
                username = "",
                password = "",
            }),
        };

        request.Headers.TryAddWithoutValidation(CorrelationHeaders.CorrelationId, "corr-int-001");

        var response = await _client.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<ApiErrorResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("corr-int-001", response.Headers.GetValues(CorrelationHeaders.CorrelationId).Single());
        Assert.NotNull(payload);
        Assert.Equal("corr-int-001", payload!.TraceId);
    }
}
