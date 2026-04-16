using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Common.Validation;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Application.Rates.UseCases.GetLatestRates;
using CurrencyApi.Application.Rates.Validation;
using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.UnitTests.Application.Rates.UseCases;

public sealed class GetLatestRatesUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnMappedLatestRatesResponse()
    {
        var provider = new StubCurrencyRatesProvider();
        var useCase = new GetLatestRatesUseCase(
            providerFactory: new StubCurrencyProviderFactory(provider),
            validator: new LatestRatesRequestValidator());

        var result = await useCase.ExecuteAsync(new LatestRatesRequest("EUR"));

        Assert.Equal("EUR", result.BaseCurrency);
        Assert.Equal(new DateOnly(2024, 1, 31), result.AsOf);
        Assert.Equal(2, result.Rates.Count);
        Assert.Equal(0.85m, result.Rates["GBP"]);
        Assert.Equal(1.08m, result.Rates["USD"]);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowRequestValidationException_WhenRequestIsInvalid()
    {
        var provider = new StubCurrencyRatesProvider();
        var useCase = new GetLatestRatesUseCase(
            providerFactory: new StubCurrencyProviderFactory(provider),
            validator: new LatestRatesRequestValidator());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => useCase.ExecuteAsync(new LatestRatesRequest("EURO")));

        Assert.Contains(exception.Errors, error => error.Code == "validation.invalid_currency");
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
        public CurrencyProviderKind Kind => CurrencyProviderKind.Frankfurter;

        public Task<LatestRatesProviderResult> GetLatestRatesAsync(
            LatestRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new LatestRatesProviderResult(
                BaseCurrency: CurrencyCode.Create("EUR"),
                AsOf: new DateOnly(2024, 1, 31),
                Rates: new Dictionary<CurrencyCode, decimal>
                {
                    [CurrencyCode.Create("USD")] = 1.08m,
                    [CurrencyCode.Create("GBP")] = 0.85m,
                }));
        }

        public Task<HistoricalRatesProviderResult> GetHistoricalRatesAsync(
            HistoricalRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
