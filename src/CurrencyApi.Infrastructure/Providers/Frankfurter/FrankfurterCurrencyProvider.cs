using System.Globalization;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Domain.Currencies;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Exceptions;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Models;

namespace CurrencyApi.Infrastructure.Providers.Frankfurter;

public sealed class FrankfurterCurrencyProvider : ICurrencyRatesProvider
{
    private readonly IFrankfurterApiClient _apiClient;

    public FrankfurterCurrencyProvider(IFrankfurterApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public CurrencyProviderKind Kind => CurrencyProviderKind.Frankfurter;

    public async Task<LatestRatesProviderResult> GetLatestRatesAsync(
        LatestRatesProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetLatestRatesAsync(request, cancellationToken);

        return new LatestRatesProviderResult(
            BaseCurrency: ParseCurrencyCode(response.Base, "latest.base"),
            AsOf: ParseDate(response.Date, "latest.date"),
            Rates: MapRates(response.Rates, "latest.rates"));
    }

    public async Task<HistoricalRatesProviderResult> GetHistoricalRatesAsync(
        HistoricalRatesProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetHistoricalRatesAsync(request, cancellationToken);

        var items = response.Rates?
            .Select(pair => new HistoricalRateSnapshot(
                Date: ParseDate(pair.Key, "historical.rates.date"),
                Rates: MapRates(pair.Value, $"historical.rates[{pair.Key}]")))
            .OrderByDescending(item => item.Date)
            .ToArray() ?? [];

        return new HistoricalRatesProviderResult(
            BaseCurrency: ParseCurrencyCode(response.Base, "historical.base"),
            StartDate: ParseDate(response.StartDate, "historical.start_date"),
            EndDate: ParseDate(response.EndDate, "historical.end_date"),
            Items: items);
    }

    private static IReadOnlyDictionary<CurrencyCode, decimal> MapRates(
        IReadOnlyDictionary<string, decimal>? rawRates,
        string source)
    {
        if (rawRates is null)
        {
            throw new FrankfurterContractException($"Frankfurter payload is missing '{source}'.");
        }

        var result = new Dictionary<CurrencyCode, decimal>();

        foreach (var pair in rawRates)
        {
            var currencyCode = ParseCurrencyCode(pair.Key, source);
            result[currencyCode] = pair.Value;
        }

        return result;
    }

    private static CurrencyCode ParseCurrencyCode(string? value, string source)
    {
        if (!CurrencyCode.TryCreate(value, out var currencyCode, out _))
        {
            throw new FrankfurterContractException($"Frankfurter payload contains an invalid currency code in '{source}'.");
        }

        return currencyCode;
    }

    private static DateOnly ParseDate(string? value, string source)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            throw new FrankfurterContractException($"Frankfurter payload contains an invalid date in '{source}'.");
        }

        return date;
    }
}
