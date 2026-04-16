using CurrencyApi.Application.Common.Validation;

namespace CurrencyApi.Application.Common.Exceptions;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(IReadOnlyList<ValidationError> errors)
        : base(errors.Count > 0 ? errors[0].Message : "The request is invalid.")
    {
        Errors = errors;
    }

    public IReadOnlyList<ValidationError> Errors { get; }
}
