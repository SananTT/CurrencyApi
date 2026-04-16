using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Infrastructure.Integrations.Frankfurter;

public static class FrankfurterRequestUriFactory
{
    public static string BuildLatest(LatestRatesProviderRequest request)
    {
        var query = BuildCommonQuery(request.BaseCurrency.Value, request.Symbols);
        return string.IsNullOrEmpty(query) ? "latest" : $"latest?{query}";
    }

    public static string BuildHistorical(HistoricalRatesProviderRequest request)
    {
        var range = $"{request.StartDate:yyyy-MM-dd}..{request.EndDate:yyyy-MM-dd}";

        var query = BuildCommonQuery(request.BaseCurrency.Value, request.Symbols);
        return string.IsNullOrEmpty(query) ? range : $"{range}?{query}";
    }

    private static string BuildCommonQuery(
        string baseCurrency,
        IReadOnlyCollection<CurrencyCode>? symbols)
    {
        var segments = new List<string>
        {
            $"from={Uri.EscapeDataString(baseCurrency)}",
        };

        if (symbols is { Count: > 0 })
        {
            var symbolList = string.Join(",", symbols.Select(symbol => symbol.Value));
            segments.Add($"to={Uri.EscapeDataString(symbolList)}");
        }

        return string.Join("&", segments);
    }
}
