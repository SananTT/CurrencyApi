using System.Net;
using System.Net.Http.Json;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Configuration;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Exceptions;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Models;
using Microsoft.Extensions.Logging;

namespace CurrencyApi.Infrastructure.Integrations.Frankfurter;

public sealed class FrankfurterApiClient : IFrankfurterApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FrankfurterApiClient> _logger;
    private readonly IFrankfurterResiliencePipeline _resiliencePipeline;

    public FrankfurterApiClient(
        HttpClient httpClient,
        ILogger<FrankfurterApiClient> logger,
        IFrankfurterResiliencePipeline resiliencePipeline,
        FrankfurterClientSettings? settings = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _resiliencePipeline = resiliencePipeline;

        var baseUrl = settings?.BaseUrl ?? FrankfurterClientSettings.DefaultBaseUrl;
        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
        }
    }

    public async Task<FrankfurterLatestRatesResponse> GetLatestRatesAsync(
        LatestRatesProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var relativeUri = FrankfurterRequestUriFactory.BuildLatest(request);
        return await _resiliencePipeline.ExecuteAsync(
            async executeCancellationToken =>
            {
                _logger.LogInformation(
                    "Sending Frankfurter latest request. RelativeUri={RelativeUri}",
                    relativeUri);

                using var response = await _httpClient.GetAsync(relativeUri, executeCancellationToken);
                _logger.LogInformation(
                    "Frankfurter latest request completed. RelativeUri={RelativeUri} StatusCode={StatusCode}",
                    relativeUri,
                    (int)response.StatusCode);
                EnsureSuccessStatusCode(response, relativeUri);

                var payload = await response.Content.ReadFromJsonAsync<FrankfurterLatestRatesResponse>(executeCancellationToken);
                return payload ?? throw new FrankfurterContractException("Frankfurter returned an empty latest rates payload.");
            },
            cancellationToken);
    }

    public async Task<FrankfurterHistoricalRatesResponse> GetHistoricalRatesAsync(
        HistoricalRatesProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var relativeUri = FrankfurterRequestUriFactory.BuildHistorical(request);
        return await _resiliencePipeline.ExecuteAsync(
            async executeCancellationToken =>
            {
                _logger.LogInformation(
                    "Sending Frankfurter historical request. RelativeUri={RelativeUri}",
                    relativeUri);

                using var response = await _httpClient.GetAsync(relativeUri, executeCancellationToken);
                _logger.LogInformation(
                    "Frankfurter historical request completed. RelativeUri={RelativeUri} StatusCode={StatusCode}",
                    relativeUri,
                    (int)response.StatusCode);
                EnsureSuccessStatusCode(response, relativeUri);

                var payload = await response.Content.ReadFromJsonAsync<FrankfurterHistoricalRatesResponse>(executeCancellationToken);
                return payload ?? throw new FrankfurterContractException("Frankfurter returned an empty historical rates payload.");
            },
            cancellationToken);
    }

    private static void EnsureSuccessStatusCode(HttpResponseMessage response, string relativeUri)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = $"Frankfurter request to '{relativeUri}' failed with status code {(int)response.StatusCode}.";

        if (IsTransientStatusCode(response.StatusCode))
        {
            throw new HttpRequestException(message, inner: null, response.StatusCode);
        }

        throw new FrankfurterUpstreamException(response.StatusCode, message);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is
            HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout ||
            (int)statusCode >= 500;
    }
}
