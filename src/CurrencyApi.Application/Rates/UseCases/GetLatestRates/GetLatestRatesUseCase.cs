using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Common.Validation;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Application.Rates.UseCases.GetLatestRates;

public sealed class GetLatestRatesUseCase : IGetLatestRatesUseCase
{
    private readonly ICurrencyProviderFactory _providerFactory;
    private readonly IValidator<LatestRatesRequest> _validator;

    public GetLatestRatesUseCase(
        ICurrencyProviderFactory providerFactory,
        IValidator<LatestRatesRequest> validator)
    {
        _providerFactory = providerFactory;
        _validator = validator;
    }

    public async Task<LatestRatesResponse> ExecuteAsync(
        LatestRatesRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors);
        }

        var provider = _providerFactory.Create(CurrencyProviderKind.Frankfurter);
        var providerRequest = new LatestRatesProviderRequest(CurrencyCode.Create(request.BaseCurrency));
        var providerResult = await provider.GetLatestRatesAsync(providerRequest, cancellationToken);

        var rates = providerResult.Rates
            .OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key.Value, pair => pair.Value, StringComparer.Ordinal);

        return new LatestRatesResponse(
            BaseCurrency: providerResult.BaseCurrency.Value,
            AsOf: providerResult.AsOf,
            Rates: rates);
    }
}
