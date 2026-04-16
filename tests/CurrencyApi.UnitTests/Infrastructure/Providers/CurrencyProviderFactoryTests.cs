using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Domain.Currencies;
using CurrencyApi.Infrastructure.Providers;

namespace CurrencyApi.UnitTests.Infrastructure.Providers;

public sealed class CurrencyProviderFactoryTests
{
    [Fact]
    public void Create_ShouldReturnMatchingProvider_WhenProviderExists()
    {
        var frankfurterProvider = new StubCurrencyRatesProvider(CurrencyProviderKind.Frankfurter);
        var factory = new CurrencyProviderFactory([frankfurterProvider]);

        var result = factory.Create(CurrencyProviderKind.Frankfurter);

        Assert.Same(frankfurterProvider, result);
    }

    [Fact]
    public void Create_ShouldThrow_WhenProviderIsMissing()
    {
        var factory = new CurrencyProviderFactory([]);

        var exception = Assert.Throws<KeyNotFoundException>(() => factory.Create((CurrencyProviderKind)999));

        Assert.Contains("999", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenMultipleProvidersHaveSameKind()
    {
        var providers = new ICurrencyRatesProvider[]
        {
            new StubCurrencyRatesProvider(CurrencyProviderKind.Frankfurter),
            new StubCurrencyRatesProvider(CurrencyProviderKind.Frankfurter),
        };

        var exception = Assert.Throws<InvalidOperationException>(() => new CurrencyProviderFactory(providers));

        Assert.Contains(nameof(CurrencyProviderKind.Frankfurter), exception.Message, StringComparison.Ordinal);
    }

    private sealed class StubCurrencyRatesProvider : ICurrencyRatesProvider
    {
        public StubCurrencyRatesProvider(CurrencyProviderKind kind)
        {
            Kind = kind;
        }

        public CurrencyProviderKind Kind { get; }

        public Task<LatestRatesProviderResult> GetLatestRatesAsync(
            LatestRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LatestRatesProviderResult(
                BaseCurrency: CurrencyCode.Create("EUR"),
                AsOf: new DateOnly(2024, 1, 1),
                Rates: new Dictionary<CurrencyCode, decimal>()));
        }

        public Task<HistoricalRatesProviderResult> GetHistoricalRatesAsync(
            HistoricalRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HistoricalRatesProviderResult(
                BaseCurrency: CurrencyCode.Create("EUR"),
                StartDate: new DateOnly(2024, 1, 1),
                EndDate: new DateOnly(2024, 1, 1),
                Items: []));
        }
    }
}
