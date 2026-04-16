using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace CurrencyApi.UnitTests.Infrastructure.Caching;

public sealed class RatesCacheInvalidatorTests
{
    [Fact]
    public async Task InvalidateAsync_ShouldRemoveTrackedEntries_ForSpecificProvider()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var registry = new RatesCacheKeyRegistry();
        registry.Track(CurrencyProviderKind.Frankfurter, "rates:Frankfurter:latest:EUR:*");
        memoryCache.Set("rates:Frankfurter:latest:EUR:*", 123m);

        var invalidator = new RatesCacheInvalidator(memoryCache, registry);

        var result = await invalidator.InvalidateAsync(CurrencyProviderKind.Frankfurter);

        Assert.Equal(1, result);
        Assert.False(memoryCache.TryGetValue("rates:Frankfurter:latest:EUR:*", out _));
    }

    [Fact]
    public async Task InvalidateAsync_ShouldThrow_WhenCancellationWasRequested()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var invalidator = new RatesCacheInvalidator(memoryCache, new RatesCacheKeyRegistry());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            invalidator.InvalidateAsync(null, cts.Token));
    }
}
