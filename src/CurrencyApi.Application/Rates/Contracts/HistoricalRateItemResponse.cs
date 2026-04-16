namespace CurrencyApi.Application.Rates.Contracts;

public sealed record HistoricalRateItemResponse(
    DateOnly Date,
    IReadOnlyDictionary<string, decimal> Rates);
