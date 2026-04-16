namespace CurrencyApi.Application.Rates.Contracts;

public sealed record LatestRatesResponse(
    string BaseCurrency,
    DateOnly AsOf,
    IReadOnlyDictionary<string, decimal> Rates);
