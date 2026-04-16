namespace CurrencyApi.Application.Rates.Contracts;

public sealed record CurrencyConversionResponse(
    string FromCurrency,
    string ToCurrency,
    decimal Amount,
    decimal Rate,
    decimal ConvertedAmount);
