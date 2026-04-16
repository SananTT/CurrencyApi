using CurrencyApi.Application.Admin.Contracts;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Domain.Currencies;
using CurrencyApi.Infrastructure.Caching;
using CurrencyApi.Infrastructure.Caching.Configuration;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyApi.UnitTests.Infrastructure.Caching;

public sealed class CachedCurrencyRatesProviderTests
{
    [Fact]
    public async Task GetLatestRatesAsync_ShouldUseCache_WhenSameRequestRepeats()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var innerProvider = new StubCurrencyRatesProvider();
        var provider = CreateProvider(memoryCache, innerProvider, out _);
        var request = new LatestRatesProviderRequest(
            BaseCurrency: CurrencyCode.Create("EUR"),
            Symbols: [CurrencyCode.Create("USD")]);

        var first = await provider.GetLatestRatesAsync(request);
        var second = await provider.GetLatestRatesAsync(request);

        Assert.Equal(1, innerProvider.LatestCallCount);
        Assert.NotSame(first, second);
        Assert.Equal(first.Rates[CurrencyCode.Create("USD")], second.Rates[CurrencyCode.Create("USD")]);
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ShouldTreatDifferentSymbolOrderAsSameCacheKey()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var innerProvider = new StubCurrencyRatesProvider();
        var provider = CreateProvider(memoryCache, innerProvider, out _);

        var firstRequest = new HistoricalRatesProviderRequest(
            BaseCurrency: CurrencyCode.Create("EUR"),
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 3),
            Symbols: [CurrencyCode.Create("USD"), CurrencyCode.Create("GBP")]);

        var secondRequest = new HistoricalRatesProviderRequest(
            BaseCurrency: CurrencyCode.Create("EUR"),
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 3),
            Symbols: [CurrencyCode.Create("GBP"), CurrencyCode.Create("USD")]);

        await provider.GetHistoricalRatesAsync(firstRequest);
        await provider.GetHistoricalRatesAsync(secondRequest);

        Assert.Equal(1, innerProvider.HistoricalCallCount);
    }

    [Fact]
    public async Task InvalidateAsync_ShouldClearTrackedEntries_AndForceRefetch()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var innerProvider = new StubCurrencyRatesProvider();
        var provider = CreateProvider(memoryCache, innerProvider, out var invalidator);

        var latestRequest = new LatestRatesProviderRequest(CurrencyCode.Create("EUR"));
        var historicalRequest = new HistoricalRatesProviderRequest(
            BaseCurrency: CurrencyCode.Create("EUR"),
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 2));

        await provider.GetLatestRatesAsync(latestRequest);
        await provider.GetHistoricalRatesAsync(historicalRequest);

        var invalidatedCount = await invalidator.InvalidateAsync(CurrencyProviderKind.Frankfurter);

        await provider.GetLatestRatesAsync(latestRequest);
        await provider.GetHistoricalRatesAsync(historicalRequest);

        Assert.Equal(2, invalidatedCount);
        Assert.Equal(2, innerProvider.LatestCallCount);
        Assert.Equal(2, innerProvider.HistoricalCallCount);
    }

    private static CachedCurrencyRatesProvider CreateProvider(
        IMemoryCache memoryCache,
        StubCurrencyRatesProvider innerProvider,
        out IRatesCacheInvalidator invalidator)
    {
        var keyRegistry = new RatesCacheKeyRegistry();
        invalidator = new RatesCacheInvalidator(memoryCache, keyRegistry);

        return new CachedCurrencyRatesProvider(
            innerProvider,
            memoryCache,
            keyRegistry,
            new RatesCacheSettings
            {
                LatestTtlSeconds = 300,
                HistoricalTtlSeconds = 300,
            });
    }

    private sealed class StubCurrencyRatesProvider : ICurrencyRatesProvider
    {
        public int LatestCallCount { get; private set; }

        public int HistoricalCallCount { get; private set; }

        public CurrencyProviderKind Kind => CurrencyProviderKind.Frankfurter;

        public Task<LatestRatesProviderResult> GetLatestRatesAsync(
            LatestRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            LatestCallCount++;

            var symbols = request.Symbols?.Count > 0
                ? request.Symbols
                : [CurrencyCode.Create("USD")];

            var rates = symbols.ToDictionary(symbol => symbol, _ => 1.25m);

            return Task.FromResult(new LatestRatesProviderResult(
                BaseCurrency: request.BaseCurrency,
                AsOf: new DateOnly(2024, 1, 1),
                Rates: rates));
        }

        public Task<HistoricalRatesProviderResult> GetHistoricalRatesAsync(
            HistoricalRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            HistoricalCallCount++;

            var items = new[]
            {
                new HistoricalRateSnapshot(
                    Date: request.EndDate,
                    Rates: new Dictionary<CurrencyCode, decimal>
                    {
                        [CurrencyCode.Create("USD")] = 1.20m,
                    }),
            };

            return Task.FromResult(new HistoricalRatesProviderResult(
                BaseCurrency: request.BaseCurrency,
                StartDate: request.StartDate,
                EndDate: request.EndDate,
                Items: items));
        }
    }
}
