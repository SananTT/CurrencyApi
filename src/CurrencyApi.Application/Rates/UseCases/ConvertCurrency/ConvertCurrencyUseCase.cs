using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Common.Validation;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Application.Rates.UseCases.ConvertCurrency;

public sealed class ConvertCurrencyUseCase : IConvertCurrencyUseCase
{
    private readonly ICurrencyProviderFactory _providerFactory;
    private readonly IValidator<ConvertCurrencyRequest> _validator;

    public ConvertCurrencyUseCase(
        ICurrencyProviderFactory providerFactory,
        IValidator<ConvertCurrencyRequest> validator)
    {
        _providerFactory = providerFactory;
        _validator = validator;
    }

    public async Task<CurrencyConversionResponse> ExecuteAsync(
        ConvertCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors);
        }

        var fromCurrency = CurrencyCode.Create(request.FromCurrency);
        var toCurrency = CurrencyCode.Create(request.ToCurrency);

        if (fromCurrency == toCurrency)
        {
            return new CurrencyConversionResponse(
                FromCurrency: fromCurrency.Value,
                ToCurrency: toCurrency.Value,
                Amount: request.Amount,
                Rate: 1m,
                ConvertedAmount: request.Amount);
        }

        var provider = _providerFactory.Create(CurrencyProviderKind.Frankfurter);
        var providerResult = await provider.GetLatestRatesAsync(
            new LatestRatesProviderRequest(fromCurrency, [toCurrency]),
            cancellationToken);

        if (!providerResult.Rates.TryGetValue(toCurrency, out var rate))
        {
            throw new InvalidOperationException(
                $"No conversion rate was returned for '{fromCurrency.Value}' to '{toCurrency.Value}'.");
        }

        return new CurrencyConversionResponse(
            FromCurrency: fromCurrency.Value,
            ToCurrency: toCurrency.Value,
            Amount: request.Amount,
            Rate: rate,
            ConvertedAmount: request.Amount * rate);
    }
}
