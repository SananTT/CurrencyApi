using CurrencyApi.Application.Providers.Contracts;

namespace CurrencyApi.Application.Admin.Contracts;

public sealed record ClearCacheResponse(
    CurrencyProviderKind? ProviderKind,
    int InvalidatedEntries);
