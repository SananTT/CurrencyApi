using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Application.Providers.Contracts;

public sealed record LatestRatesProviderResult(
    CurrencyCode BaseCurrency,
    DateOnly AsOf,
    IReadOnlyDictionary<CurrencyCode, decimal> Rates);
