using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Infrastructure.Caching;

namespace CurrencyApi.UnitTests.Infrastructure.Caching;

public sealed class RatesCacheKeyRegistryTests
{
    [Fact]
    public void Snapshot_ShouldReturnTrackedKeysAcrossAllProviders_InStableOrder()
    {
        var registry = new RatesCacheKeyRegistry();

        registry.Track(CurrencyProviderKind.Frankfurter, "b-key");
        registry.Track(CurrencyProviderKind.Frankfurter, "a-key");

        var result = registry.Snapshot(null);

        Assert.Equal(new[] { "a-key", "b-key" }, result);
    }

    [Fact]
    public void Untrack_ShouldRemoveProviderBucket_WhenLastKeyIsRemoved()
    {
        var registry = new RatesCacheKeyRegistry();
        registry.Track(CurrencyProviderKind.Frankfurter, "a-key");

        registry.Untrack(CurrencyProviderKind.Frankfurter, "a-key");

        Assert.Empty(registry.Snapshot(CurrencyProviderKind.Frankfurter));
    }
}
