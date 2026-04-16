using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Infrastructure.Caching;

public static class RatesCacheKeyFactory
{
    public static string CreateLatestKey(
        CurrencyProviderKind providerKind,
        LatestRatesProviderRequest request) =>
        FormattableString.Invariant(
            $"rates:{providerKind}:latest:{request.BaseCurrency.Value}:{FormatSymbols(request.Symbols)}");

    public static string CreateHistoricalKey(
        CurrencyProviderKind providerKind,
        HistoricalRatesProviderRequest request) =>
        FormattableString.Invariant(
            $"rates:{providerKind}:historical:{request.BaseCurrency.Value}:{request.StartDate:yyyy-MM-dd}:{request.EndDate:yyyy-MM-dd}:{FormatSymbols(request.Symbols)}");

    private static string FormatSymbols(IReadOnlyCollection<CurrencyCode>? symbols)
    {
        if (symbols is null || symbols.Count == 0)
        {
            return "*";
        }

        return string.Join(
            ",",
            symbols
                .Select(symbol => symbol.Value)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal));
    }
}
