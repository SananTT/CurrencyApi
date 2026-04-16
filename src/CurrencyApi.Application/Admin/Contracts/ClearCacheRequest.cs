using CurrencyApi.Application.Providers.Contracts;

namespace CurrencyApi.Application.Admin.Contracts;

public sealed record ClearCacheRequest(CurrencyProviderKind? ProviderKind = null);
