using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Domain.Currencies;
using CurrencyApi.Infrastructure.Integrations.Frankfurter;

namespace CurrencyApi.UnitTests.Infrastructure.Integrations.Frankfurter;

public sealed class FrankfurterRequestUriFactoryTests
{
    [Fact]
    public void BuildLatest_ShouldGenerate_FromOnlyQuery_WhenSymbolsAreMissing()
    {
        var request = new LatestRatesProviderRequest(CurrencyCode.Create("EUR"));

        var relativeUri = FrankfurterRequestUriFactory.BuildLatest(request);

        Assert.Equal("latest?from=EUR", relativeUri);
    }

    [Fact]
    public void BuildLatest_ShouldGenerate_FromAndToQuery_WhenSymbolsExist()
    {
        var request = new LatestRatesProviderRequest(
            CurrencyCode.Create("EUR"),
            [CurrencyCode.Create("USD"), CurrencyCode.Create("GBP")]);

        var relativeUri = FrankfurterRequestUriFactory.BuildLatest(request);

        Assert.Equal("latest?from=EUR&to=USD%2CGBP", relativeUri);
    }

    [Fact]
    public void BuildHistorical_ShouldGenerate_DateRangeAndQuery()
    {
        var request = new HistoricalRatesProviderRequest(
            BaseCurrency: CurrencyCode.Create("EUR"),
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 31),
            Symbols: [CurrencyCode.Create("USD")]);

        var relativeUri = FrankfurterRequestUriFactory.BuildHistorical(request);

        Assert.Equal("2024-01-01..2024-01-31?from=EUR&to=USD", relativeUri);
    }
}
