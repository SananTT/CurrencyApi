using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Domain.Currencies;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Models;
using CurrencyApi.Infrastructure.Providers.Frankfurter;

namespace CurrencyApi.UnitTests.Infrastructure.Providers.Frankfurter;

public sealed class FrankfurterCurrencyProviderTests
{
    [Fact]
    public async Task GetLatestRatesAsync_ShouldMapPayloadIntoProviderResult()
    {
        var client = new StubFrankfurterApiClient
        {
            LatestResponse = new FrankfurterLatestRatesResponse
            {
                Base = "EUR",
                Date = "2024-01-31",
                Rates = new Dictionary<string, decimal>
                {
                    ["USD"] = 1.08m,
                    ["GBP"] = 0.85m,
                },
            },
        };

        var provider = new FrankfurterCurrencyProvider(client);
        var result = await provider.GetLatestRatesAsync(new LatestRatesProviderRequest(CurrencyCode.Create("EUR")));

        Assert.Equal(CurrencyProviderKind.Frankfurter, provider.Kind);
        Assert.Equal("EUR", result.BaseCurrency.Value);
        Assert.Equal(new DateOnly(2024, 1, 31), result.AsOf);
        Assert.Equal(2, result.Rates.Count);
        Assert.Equal(1.08m, result.Rates[CurrencyCode.Create("USD")]);
    }

    [Fact]
    public async Task GetHistoricalRatesAsync_ShouldMapAndSortSnapshotsDescending()
    {
        var client = new StubFrankfurterApiClient
        {
            HistoricalResponse = new FrankfurterHistoricalRatesResponse
            {
                Base = "EUR",
                StartDate = "2024-01-01",
                EndDate = "2024-01-03",
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    ["2024-01-01"] = new Dictionary<string, decimal> { ["USD"] = 1.05m },
                    ["2024-01-03"] = new Dictionary<string, decimal> { ["USD"] = 1.08m },
                    ["2024-01-02"] = new Dictionary<string, decimal> { ["USD"] = 1.06m },
                },
            },
        };

        var provider = new FrankfurterCurrencyProvider(client);
        var result = await provider.GetHistoricalRatesAsync(new HistoricalRatesProviderRequest(
            BaseCurrency: CurrencyCode.Create("EUR"),
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 3)));

        Assert.Equal("EUR", result.BaseCurrency.Value);
        Assert.Equal(new DateOnly(2024, 1, 1), result.StartDate);
        Assert.Equal(new DateOnly(2024, 1, 3), result.EndDate);
        Assert.Equal(3, result.Items.Count);
        Assert.Equal(new DateOnly(2024, 1, 3), result.Items[0].Date);
        Assert.Equal(new DateOnly(2024, 1, 2), result.Items[1].Date);
        Assert.Equal(new DateOnly(2024, 1, 1), result.Items[2].Date);
        Assert.Equal(1.08m, result.Items[0].Rates[CurrencyCode.Create("USD")]);
    }

    private sealed class StubFrankfurterApiClient : IFrankfurterApiClient
    {
        public FrankfurterLatestRatesResponse? LatestResponse { get; init; }

        public FrankfurterHistoricalRatesResponse? HistoricalResponse { get; init; }

        public Task<FrankfurterLatestRatesResponse> GetLatestRatesAsync(
            LatestRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LatestResponse ?? throw new InvalidOperationException("LatestResponse was not configured."));
        }

        public Task<FrankfurterHistoricalRatesResponse> GetHistoricalRatesAsync(
            HistoricalRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HistoricalResponse ?? throw new InvalidOperationException("HistoricalResponse was not configured."));
        }
    }
}
