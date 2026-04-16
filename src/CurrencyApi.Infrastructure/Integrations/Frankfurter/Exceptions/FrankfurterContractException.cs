namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Exceptions;

public sealed class FrankfurterContractException : Exception
{
    public FrankfurterContractException(string message)
        : base(message)
    {
    }
}
