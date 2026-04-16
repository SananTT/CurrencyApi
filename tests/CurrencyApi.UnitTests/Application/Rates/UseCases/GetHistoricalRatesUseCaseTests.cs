using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Application.Rates.UseCases.GetHistoricalRates;
using CurrencyApi.Application.Rates.Validation;
using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.UnitTests.Application.Rates.UseCases;

public sealed class GetHistoricalRatesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnDateCentricDescendingItems()
    {
        var provider = new StubCurrencyRatesProvider();
        var useCase = new GetHistoricalRatesUseCase(
            providerFactory: new StubCurrencyProviderFactory(provider),
            validator: new HistoricalRatesRequestValidator());

        var result = await useCase.ExecuteAsync(new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 3),
            Page: 1,
            PageSize: 2,
            Symbols: ["USD", "GBP"]));

        Assert.Equal("EUR", result.BaseCurrency);
        Assert.Equal("date_desc", result.Sort);
        Assert.Equal(3, result.Page.TotalItems);
        Assert.Equal(2, result.Page.TotalPages);
        Assert.Equal(2, result.Page.Items.Count);
        Assert.Equal(new DateOnly(2024, 1, 3), result.Page.Items[0].Date);
        Assert.Equal(1.08m, result.Page.Items[0].Rates["USD"]);
        Assert.Equal(0.85m, result.Page.Items[0].Rates["GBP"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnRequestedPageSlice()
    {
        var provider = new StubCurrencyRatesProvider();
        var useCase = new GetHistoricalRatesUseCase(
            providerFactory: new StubCurrencyProviderFactory(provider),
            validator: new HistoricalRatesRequestValidator());

        var result = await useCase.ExecuteAsync(new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 3),
            Page: 2,
            PageSize: 2,
            Symbols: ["USD"]));

        Assert.Equal(2, result.Page.Page);
        Assert.Single(result.Page.Items);
        Assert.Equal(new DateOnly(2024, 1, 1), result.Page.Items[0].Date);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPassNormalizedSymbolsToProvider()
    {
        var provider = new StubCurrencyRatesProvider();
        var useCase = new GetHistoricalRatesUseCase(
            providerFactory: new StubCurrencyProviderFactory(provider),
            validator: new HistoricalRatesRequestValidator());

        await useCase.ExecuteAsync(new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 3),
            Symbols: ["usd", " gbp "]));

        Assert.NotNull(provider.LastHistoricalRequest);
        Assert.Equal("EUR", provider.LastHistoricalRequest!.BaseCurrency.Value);
        Assert.Equal(["USD", "GBP"], provider.LastHistoricalRequest.Symbols!.Select(symbol => symbol.Value).ToArray());
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowRequestValidationException_WhenRequestIsInvalid()
    {
        var provider = new StubCurrencyRatesProvider();
        var useCase = new GetHistoricalRatesUseCase(
            providerFactory: new StubCurrencyProviderFactory(provider),
            validator: new HistoricalRatesRequestValidator());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => useCase.ExecuteAsync(
            new HistoricalRatesRequest(
                BaseCurrency: "EUR",
                StartDate: new DateOnly(2024, 1, 3),
                EndDate: new DateOnly(2024, 1, 1))));

        Assert.Contains(exception.Errors, error => error.Code == "validation.date_range_invalid");
    }

    private sealed class StubCurrencyProviderFactory : ICurrencyProviderFactory
    {
        private readonly ICurrencyRatesProvider _provider;

        public StubCurrencyProviderFactory(ICurrencyRatesProvider provider)
        {
            _provider = provider;
        }

        public ICurrencyRatesProvider Create(CurrencyProviderKind providerKind) => _provider;
    }

    private sealed class StubCurrencyRatesProvider : ICurrencyRatesProvider
    {
        public HistoricalRatesProviderRequest? LastHistoricalRequest { get; private set; }

        public CurrencyProviderKind Kind => CurrencyProviderKind.Frankfurter;

        public Task<LatestRatesProviderResult> GetLatestRatesAsync(
            LatestRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<HistoricalRatesProviderResult> GetHistoricalRatesAsync(
            HistoricalRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            LastHistoricalRequest = request;

            return Task.FromResult(new HistoricalRatesProviderResult(
                BaseCurrency: request.BaseCurrency,
                StartDate: request.StartDate,
                EndDate: request.EndDate,
                Items:
                [
                    new HistoricalRateSnapshot(
                        new DateOnly(2024, 1, 3),
                        new Dictionary<CurrencyCode, decimal>
                        {
                            [CurrencyCode.Create("USD")] = 1.08m,
                            [CurrencyCode.Create("GBP")] = 0.85m,
                        }),
                    new HistoricalRateSnapshot(
                        new DateOnly(2024, 1, 2),
                        new Dictionary<CurrencyCode, decimal>
                        {
                            [CurrencyCode.Create("USD")] = 1.07m,
                            [CurrencyCode.Create("GBP")] = 0.84m,
                        }),
                    new HistoricalRateSnapshot(
                        new DateOnly(2024, 1, 1),
                        new Dictionary<CurrencyCode, decimal>
                        {
                            [CurrencyCode.Create("USD")] = 1.06m,
                            [CurrencyCode.Create("GBP")] = 0.83m,
                        }),
                ]));
        }
    }
}
