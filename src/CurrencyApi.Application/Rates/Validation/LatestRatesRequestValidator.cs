using CurrencyApi.Application.Common.Validation;
using CurrencyApi.Application.Rates.Contracts;

namespace CurrencyApi.Application.Rates.Validation;

public sealed class LatestRatesRequestValidator : IValidator<LatestRatesRequest>
{
    public ValidationResult Validate(LatestRatesRequest request)
    {
        var errors = new List<ValidationError>();

        CurrencyValidation.ValidateCurrency(request.BaseCurrency, nameof(request.BaseCurrency), errors);

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.WithErrors(errors);
    }
}
