using CurrencyApi.Application.Common.Validation;
using CurrencyApi.Application.Rates.Contracts;

namespace CurrencyApi.Application.Rates.Validation;

public sealed class ConvertCurrencyRequestValidator : IValidator<ConvertCurrencyRequest>
{
    public ValidationResult Validate(ConvertCurrencyRequest request)
    {
        var errors = new List<ValidationError>();

        CurrencyValidation.ValidateCurrency(request.FromCurrency, nameof(request.FromCurrency), errors, rejectExcluded: true);
        CurrencyValidation.ValidateCurrency(request.ToCurrency, nameof(request.ToCurrency), errors, rejectExcluded: true);

        if (request.Amount <= 0)
        {
            errors.Add(ValidationErrorFactory.AmountMustBePositive(nameof(request.Amount)));
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.WithErrors(errors);
    }
}
