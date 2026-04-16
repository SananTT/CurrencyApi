namespace CurrencyApi.Application.Providers.Contracts;

public interface ICurrencyRatesProvider
{
    CurrencyProviderKind Kind { get; }

    Task<LatestRatesProviderResult> GetLatestRatesAsync(
        LatestRatesProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<HistoricalRatesProviderResult> GetHistoricalRatesAsync(
        HistoricalRatesProviderRequest request,
        CancellationToken cancellationToken = default);
}
