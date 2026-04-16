using CurrencyApi.Application.Admin.Contracts;
using CurrencyApi.Application.Providers.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyApi.Infrastructure.Caching;

public sealed class RatesCacheInvalidator : IRatesCacheInvalidator
{
    private readonly IMemoryCache _memoryCache;
    private readonly RatesCacheKeyRegistry _keyRegistry;

    public RatesCacheInvalidator(
        IMemoryCache memoryCache,
        RatesCacheKeyRegistry keyRegistry)
    {
        _memoryCache = memoryCache;
        _keyRegistry = keyRegistry;
    }

    public Task<int> InvalidateAsync(
        CurrencyProviderKind? providerKind,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var keys = _keyRegistry.Snapshot(providerKind);
        foreach (var key in keys)
        {
            _memoryCache.Remove(key);
        }

        return Task.FromResult(keys.Count);
    }
}
