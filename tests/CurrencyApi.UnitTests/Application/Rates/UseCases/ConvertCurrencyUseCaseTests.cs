using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Application.Rates.UseCases.ConvertCurrency;
using CurrencyApi.Application.Rates.Validation;
using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.UnitTests.Application.Rates.UseCases;

public sealed class ConvertCurrencyUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnConvertedAmount_WhenCurrenciesDiffer()
    {
        var provider = new StubCurrencyRatesProvider();
        var useCase = new ConvertCurrencyUseCase(
            providerFactory: new StubCurrencyProviderFactory(provider),
            validator: new ConvertCurrencyRequestValidator());

        var result = await useCase.ExecuteAsync(new ConvertCurrencyRequest("USD", "AZN", 10m));

        Assert.Equal("USD", result.FromCurrency);
        Assert.Equal("AZN", result.ToCurrency);
        Assert.Equal(10m, result.Amount);
        Assert.Equal(1.7m, result.Rate);
        Assert.Equal(17m, result.ConvertedAmount);
        Assert.Equal(1, provider.GetLatestCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldNotCallProvider_WhenCurrenciesAreEqual()
    {
        var provider = new StubCurrencyRatesProvider();
        var useCase = new ConvertCurrencyUseCase(
            providerFactory: new StubCurrencyProviderFactory(provider),
            validator: new ConvertCurrencyRequestValidator());

        var result = await useCase.ExecuteAsync(new ConvertCurrencyRequest("EUR", "EUR", 10m));

        Assert.Equal(1m, result.Rate);
        Assert.Equal(10m, result.ConvertedAmount);
        Assert.Equal(0, provider.GetLatestCallCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowRequestValidationException_WhenCurrencyIsExcluded()
    {
        var provider = new StubCurrencyRatesProvider();
        var useCase = new ConvertCurrencyUseCase(
            providerFactory: new StubCurrencyProviderFactory(provider),
            validator: new ConvertCurrencyRequestValidator());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => useCase.ExecuteAsync(new ConvertCurrencyRequest("TRY", "USD", 10m)));

        Assert.Contains(exception.Errors, error => error.Code == "validation.excluded_currency");
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
        public int GetLatestCallCount { get; private set; }

        public CurrencyProviderKind Kind => CurrencyProviderKind.Frankfurter;

        public Task<LatestRatesProviderResult> GetLatestRatesAsync(
            LatestRatesProviderRequest request,
            CancellationToken cancellationToken = default)
        {
            GetLatestCallCount++;

            return Task.FromResult(new LatestRatesProviderResult(
                BaseCurrency: request.BaseCurrency,
                AsOf: new DateOnly(2024, 1, 31),
                Rates: new Dictionary<CurrencyCode, decimal>
                {
                    [CurrencyCode.Create("AZN")] = 1.7m,
                    [CurrencyCode.Create("USD")] = 1.08m,
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
