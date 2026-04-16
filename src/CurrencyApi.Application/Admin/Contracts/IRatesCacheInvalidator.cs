using CurrencyApi.Application.Providers.Contracts;

namespace CurrencyApi.Application.Admin.Contracts;

public interface IRatesCacheInvalidator
{
    Task<int> InvalidateAsync(
        CurrencyProviderKind? providerKind,
        CancellationToken cancellationToken = default);
}
