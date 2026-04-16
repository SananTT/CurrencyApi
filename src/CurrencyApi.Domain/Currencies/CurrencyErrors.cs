using CurrencyApi.Domain.Common;

namespace CurrencyApi.Domain.Currencies;

public static class CurrencyErrors
{
    public static DomainError InvalidCode(string? value)
    {
        var printableValue = string.IsNullOrWhiteSpace(value) ? "<empty>" : value.Trim();

        return new DomainError(
            Code: "currency.invalid_code",
            Message: $"Currency code '{printableValue}' is invalid. Expected a 3-letter ISO-style code.");
    }

    public static DomainError Excluded(string code) =>
        new(
            Code: "currency.excluded",
            Message: $"Currency '{code}' is excluded from this operation.");
}
