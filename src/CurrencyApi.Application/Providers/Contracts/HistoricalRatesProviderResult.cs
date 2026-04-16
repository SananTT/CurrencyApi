using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Application.Providers.Contracts;

public sealed record HistoricalRatesProviderResult(
    CurrencyCode BaseCurrency,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<HistoricalRateSnapshot> Items);
