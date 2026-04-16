using CurrencyApi.Application.Rates.Contracts;

namespace CurrencyApi.Application.Rates.UseCases.GetLatestRates;

public interface IGetLatestRatesUseCase
{
    Task<LatestRatesResponse> ExecuteAsync(
        LatestRatesRequest request,
        CancellationToken cancellationToken = default);
}
