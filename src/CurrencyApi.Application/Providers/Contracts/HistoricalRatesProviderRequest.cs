using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Application.Providers.Contracts;

public sealed record HistoricalRatesProviderRequest(
    CurrencyCode BaseCurrency,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyCollection<CurrencyCode>? Symbols = null);
