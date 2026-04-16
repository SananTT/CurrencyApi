using CurrencyApi.Application.Rates.Contracts;

namespace CurrencyApi.Application.Rates.UseCases.GetHistoricalRates;

public interface IGetHistoricalRatesUseCase
{
    Task<HistoricalRatesResponse> ExecuteAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken = default);
}
