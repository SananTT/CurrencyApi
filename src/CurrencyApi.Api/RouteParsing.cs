namespace CurrencyApi.Api;

internal static class RouteParsing
{
    public static IReadOnlyList<string>? ParseSymbols(string? symbols)
    {
        if (string.IsNullOrWhiteSpace(symbols))
        {
            return null;
        }

        return symbols
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    }
}
