namespace CurrencyApi.Application.Rates.Contracts;

public sealed record ConvertCurrencyRequest(
    string FromCurrency,
    string ToCurrency,
    decimal Amount);
