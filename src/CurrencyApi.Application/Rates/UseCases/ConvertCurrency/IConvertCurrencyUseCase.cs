using CurrencyApi.Application.Rates.Contracts;

namespace CurrencyApi.Application.Rates.UseCases.ConvertCurrency;

public interface IConvertCurrencyUseCase
{
    Task<CurrencyConversionResponse> ExecuteAsync(
        ConvertCurrencyRequest request,
        CancellationToken cancellationToken = default);
}
