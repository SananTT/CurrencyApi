namespace CurrencyApi.Domain.Currencies;

public static class ExcludedCurrencyPolicy
{
    private static readonly HashSet<string> ExcludedCodes = new(StringComparer.Ordinal)
    {
        "TRY",
        "PLN",
        "THB",
        "MXN",
    };

    public static IReadOnlySet<string> Codes => ExcludedCodes;

    public static bool IsExcluded(CurrencyCode currencyCode) =>
        ExcludedCodes.Contains(currencyCode.Value);

    public static bool IsExcluded(string? currencyCode)
    {
        if (!CurrencyCode.TryCreate(currencyCode, out var normalizedCode, out _))
        {
            return false;
        }

        return IsExcluded(normalizedCode);
    }
}
