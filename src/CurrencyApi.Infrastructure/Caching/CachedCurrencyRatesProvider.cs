using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Infrastructure.Caching.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyApi.Infrastructure.Caching;

public sealed class CachedCurrencyRatesProvider : ICurrencyRatesProvider
{
    private readonly ICurrencyRatesProvider _innerProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly RatesCacheKeyRegistry _keyRegistry;
    private readonly RatesCacheSettings _settings;

    public CachedCurrencyRatesProvider(
        ICurrencyRatesProvider innerProvider,
        IMemoryCache memoryCache,
        RatesCacheKeyRegistry keyRegistry,
        RatesCacheSettings settings)
    {
        _innerProvider = innerProvider;
        _memoryCache = memoryCache;
        _keyRegistry = keyRegistry;
        _settings = settings;
    }

    public CurrencyProviderKind Kind => _innerProvider.Kind;

    public async Task<LatestRatesProviderResult> GetLatestRatesAsync(
        LatestRatesProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = RatesCacheKeyFactory.CreateLatestKey(Kind, request);
        var cachedResult = await _memoryCache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                ConfigureEntry(entry, cacheKey, _settings.LatestTtlSeconds);
                var result = await _innerProvider.GetLatestRatesAsync(request, cancellationToken);
                _keyRegistry.Track(Kind, cacheKey);
                return Clone(result);
            });

        return Clone(cachedResult ?? throw new InvalidOperationException("Latest rates cache returned no value."));
    }

    public async Task<HistoricalRatesProviderResult> GetHistoricalRatesAsync(
        HistoricalRatesProviderRequest request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = RatesCacheKeyFactory.CreateHistoricalKey(Kind, request);
        var cachedResult = await _memoryCache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                ConfigureEntry(entry, cacheKey, _settings.HistoricalTtlSeconds);
                var result = await _innerProvider.GetHistoricalRatesAsync(request, cancellationToken);
                _keyRegistry.Track(Kind, cacheKey);
                return Clone(result);
            });

        return Clone(cachedResult ?? throw new InvalidOperationException("Historical rates cache returned no value."));
    }

    private void ConfigureEntry(ICacheEntry entry, string cacheKey, int ttlSeconds)
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(ttlSeconds);
        entry.RegisterPostEvictionCallback(
            static (key, _, _, state) =>
            {
                var context = (EvictionContext)state!;
                context.KeyRegistry.Untrack(context.ProviderKind, (string)key);
            },
            new EvictionContext(Kind, _keyRegistry));
    }

    private static LatestRatesProviderResult Clone(LatestRatesProviderResult result) =>
        new(
            BaseCurrency: result.BaseCurrency,
            AsOf: result.AsOf,
            Rates: result.Rates.ToDictionary(
                pair => pair.Key,
                pair => pair.Value));

    private static HistoricalRatesProviderResult Clone(HistoricalRatesProviderResult result) =>
        new(
            BaseCurrency: result.BaseCurrency,
            StartDate: result.StartDate,
            EndDate: result.EndDate,
            Items: result.Items
                .Select(item => new HistoricalRateSnapshot(
                    Date: item.Date,
                    Rates: item.Rates.ToDictionary(
                        pair => pair.Key,
                        pair => pair.Value)))
                .ToArray());

    private sealed record EvictionContext(
        CurrencyProviderKind ProviderKind,
        RatesCacheKeyRegistry KeyRegistry);
}
