using System.Collections.Concurrent;
using CurrencyApi.Application.Providers.Contracts;

namespace CurrencyApi.Infrastructure.Caching;

public sealed class RatesCacheKeyRegistry
{
    private readonly ConcurrentDictionary<CurrencyProviderKind, ConcurrentDictionary<string, byte>> _keysByProvider = new();

    public void Track(CurrencyProviderKind providerKind, string cacheKey)
    {
        var keys = _keysByProvider.GetOrAdd(
            providerKind,
            static _ => new ConcurrentDictionary<string, byte>(StringComparer.Ordinal));

        keys.TryAdd(cacheKey, 0);
    }

    public void Untrack(CurrencyProviderKind providerKind, string cacheKey)
    {
        if (!_keysByProvider.TryGetValue(providerKind, out var keys))
        {
            return;
        }

        keys.TryRemove(cacheKey, out _);

        if (keys.IsEmpty)
        {
            _keysByProvider.TryRemove(providerKind, out _);
        }
    }

    public IReadOnlyList<string> Snapshot(CurrencyProviderKind? providerKind)
    {
        if (providerKind.HasValue)
        {
            return Snapshot(providerKind.Value);
        }

        return _keysByProvider
            .OrderBy(pair => pair.Key)
            .SelectMany(pair => pair.Value.Keys.OrderBy(key => key, StringComparer.Ordinal))
            .ToArray();
    }

    private IReadOnlyList<string> Snapshot(CurrencyProviderKind providerKind)
    {
        if (!_keysByProvider.TryGetValue(providerKind, out var keys))
        {
            return [];
        }

        return keys.Keys
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
    }
}
