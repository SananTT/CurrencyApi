using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Models;

namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;

public interface IFrankfurterApiClient
{
    Task<FrankfurterLatestRatesResponse> GetLatestRatesAsync(
        LatestRatesProviderRequest request,
        CancellationToken cancellationToken = default);

    Task<FrankfurterHistoricalRatesResponse> GetHistoricalRatesAsync(
        HistoricalRatesProviderRequest request,
        CancellationToken cancellationToken = default);
}
