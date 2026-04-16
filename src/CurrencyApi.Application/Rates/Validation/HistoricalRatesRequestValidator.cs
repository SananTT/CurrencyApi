using CurrencyApi.Application.Common.Validation;
using CurrencyApi.Application.Rates.Contracts;

namespace CurrencyApi.Application.Rates.Validation;

public sealed class HistoricalRatesRequestValidator : IValidator<HistoricalRatesRequest>
{
    public const int MaxPageSize = 50;

    public ValidationResult Validate(HistoricalRatesRequest request)
    {
        var errors = new List<ValidationError>();

        CurrencyValidation.ValidateCurrency(request.BaseCurrency, nameof(request.BaseCurrency), errors);

        if (request.StartDate == default || request.EndDate == default)
        {
            errors.Add(ValidationErrorFactory.InvalidDateRange(
                target: $"{nameof(request.StartDate)}/{nameof(request.EndDate)}",
                message: "StartDate and EndDate are required."));
        }
        else if (request.StartDate > request.EndDate)
        {
            errors.Add(ValidationErrorFactory.InvalidDateRange(
                target: $"{nameof(request.StartDate)}/{nameof(request.EndDate)}",
                message: "StartDate must be earlier than or equal to EndDate."));
        }

        if (request.Page < 1)
        {
            errors.Add(ValidationErrorFactory.PageOutOfRange(nameof(request.Page)));
        }

        if (request.PageSize < 1 || request.PageSize > MaxPageSize)
        {
            errors.Add(ValidationErrorFactory.PageSizeOutOfRange(nameof(request.PageSize), MaxPageSize));
        }

        ValidateSymbols(request.Symbols, errors);

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.WithErrors(errors);
    }

    private static void ValidateSymbols(IReadOnlyList<string>? symbols, ICollection<ValidationError> errors)
    {
        if (symbols is null || symbols.Count == 0)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < symbols.Count; index++)
        {
            var target = $"{nameof(HistoricalRatesRequest.Symbols)}[{index}]";
            var currencyCode = CurrencyValidation.ValidateCurrency(
                input: symbols[index],
                target: target,
                errors: errors,
                rejectExcluded: true);

            if (currencyCode is null)
            {
                continue;
            }

            var normalizedCode = currencyCode.Value.Value;

            if (!seen.Add(normalizedCode))
            {
                errors.Add(ValidationErrorFactory.DuplicateSymbol(nameof(HistoricalRatesRequest.Symbols), normalizedCode));
            }
        }
    }
}
