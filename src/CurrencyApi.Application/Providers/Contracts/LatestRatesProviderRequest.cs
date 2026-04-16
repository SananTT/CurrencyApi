using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Application.Providers.Contracts;

public sealed record LatestRatesProviderRequest(
    CurrencyCode BaseCurrency,
    IReadOnlyCollection<CurrencyCode>? Symbols = null);
