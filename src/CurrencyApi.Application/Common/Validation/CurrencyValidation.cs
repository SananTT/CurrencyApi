using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Application.Common.Validation;

internal static class CurrencyValidation
{
    public static CurrencyCode? ValidateCurrency(
        string? input,
        string target,
        ICollection<ValidationError> errors,
        bool rejectExcluded = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            errors.Add(ValidationErrorFactory.Required(target));
            return null;
        }

        if (!CurrencyCode.TryCreate(input, out var currencyCode, out _))
        {
            errors.Add(ValidationErrorFactory.InvalidCurrency(target, input));
            return null;
        }

        if (rejectExcluded && currencyCode.IsExcluded)
        {
            errors.Add(ValidationErrorFactory.ExcludedCurrency(target, currencyCode.Value));
            return null;
        }

        return currencyCode;
    }
}
