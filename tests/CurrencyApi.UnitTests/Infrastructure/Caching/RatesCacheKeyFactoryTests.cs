using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Domain.Currencies;
using CurrencyApi.Infrastructure.Caching;

namespace CurrencyApi.UnitTests.Infrastructure.Caching;

public sealed class RatesCacheKeyFactoryTests
{
    [Fact]
    public void CreateLatestKey_ShouldNormalizeSymbolOrder()
    {
        var request = new LatestRatesProviderRequest(
            BaseCurrency: CurrencyCode.Create("EUR"),
            Symbols:
            [
                CurrencyCode.Create("USD"),
                CurrencyCode.Create("GBP"),
            ]);

        var key = RatesCacheKeyFactory.CreateLatestKey(CurrencyProviderKind.Frankfurter, request);

        Assert.Equal("rates:Frankfurter:latest:EUR:GBP,USD", key);
    }

    [Fact]
    public void CreateHistoricalKey_ShouldUseWildcard_WhenSymbolsAreMissing()
    {
        var request = new HistoricalRatesProviderRequest(
            BaseCurrency: CurrencyCode.Create("EUR"),
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 7),
            Symbols: null);

        var key = RatesCacheKeyFactory.CreateHistoricalKey(CurrencyProviderKind.Frankfurter, request);

        Assert.Equal("rates:Frankfurter:historical:EUR:2024-01-01:2024-01-07:*", key);
    }
}
