namespace CurrencyApi.Application.Common.Exceptions;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException()
        : base("The supplied credentials are invalid.")
    {
    }
}
