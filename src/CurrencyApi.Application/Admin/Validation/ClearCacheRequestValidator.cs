using CurrencyApi.Application.Admin.Contracts;
using CurrencyApi.Application.Common.Validation;

namespace CurrencyApi.Application.Admin.Validation;

public sealed class ClearCacheRequestValidator : IValidator<ClearCacheRequest>
{
    public ValidationResult Validate(ClearCacheRequest request)
    {
        var errors = new List<ValidationError>();

        if (request.ProviderKind.HasValue &&
            !Enum.IsDefined(typeof(Providers.Contracts.CurrencyProviderKind), request.ProviderKind.Value))
        {
            errors.Add(ValidationErrorFactory.InvalidProviderKind(nameof(request.ProviderKind), request.ProviderKind.Value));
        }

        return errors.Count == 0 ? ValidationResult.Success : ValidationResult.WithErrors(errors);
    }
}
