using CurrencyApi.Application.Auth.Contracts;
using CurrencyApi.Application.Common.Validation;

namespace CurrencyApi.Application.Auth.Validation;

public sealed class LoginRequestValidator : IValidator<LoginRequest>
{
    public ValidationResult Validate(LoginRequest request)
    {
        var errors = new List<ValidationError>();

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            errors.Add(ValidationErrorFactory.Required(nameof(request.Username)));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add(ValidationErrorFactory.Required(nameof(request.Password)));
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.WithErrors(errors);
    }
}
