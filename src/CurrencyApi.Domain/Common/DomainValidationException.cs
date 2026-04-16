namespace CurrencyApi.Domain.Common;

public sealed class DomainValidationException : Exception
{
    public DomainValidationException(DomainError error)
        : base(error.Message)
    {
        Error = error;
    }

    public DomainError Error { get; }
}
