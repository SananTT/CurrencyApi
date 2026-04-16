using System.Text.RegularExpressions;
using CurrencyApi.Domain.Common;

namespace CurrencyApi.Domain.Currencies;

public readonly partial record struct CurrencyCode
{
    private CurrencyCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public bool IsExcluded => ExcludedCurrencyPolicy.IsExcluded(this);

    public static CurrencyCode Create(string? value)
    {
        if (!TryCreate(value, out var currencyCode, out var error))
        {
            throw new DomainValidationException(error!);
        }

        return currencyCode;
    }

    public static bool TryCreate(string? value, out CurrencyCode currencyCode, out DomainError? error)
    {
        currencyCode = default;
        error = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            error = CurrencyErrors.InvalidCode(value);
            return false;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (!CurrencyCodeRegex().IsMatch(normalized))
        {
            error = CurrencyErrors.InvalidCode(value);
            return false;
        }

        currencyCode = new CurrencyCode(normalized);
        return true;
    }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyCodeRegex();
}
