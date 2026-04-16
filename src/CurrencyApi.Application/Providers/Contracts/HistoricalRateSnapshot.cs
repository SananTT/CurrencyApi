using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Application.Providers.Contracts;

public sealed record HistoricalRateSnapshot(
    DateOnly Date,
    IReadOnlyDictionary<CurrencyCode, decimal> Rates);
