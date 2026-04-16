using CurrencyApi.Application.Common.Contracts;
using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Common.Validation;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.Application.Rates.UseCases.GetHistoricalRates;

public sealed class GetHistoricalRatesUseCase : IGetHistoricalRatesUseCase
{
    private readonly ICurrencyProviderFactory _providerFactory;
    private readonly IValidator<HistoricalRatesRequest> _validator;

    public GetHistoricalRatesUseCase(
        ICurrencyProviderFactory providerFactory,
        IValidator<HistoricalRatesRequest> validator)
    {
        _providerFactory = providerFactory;
        _validator = validator;
    }

    public async Task<HistoricalRatesResponse> ExecuteAsync(
        HistoricalRatesRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors);
        }

        var baseCurrency = CurrencyCode.Create(request.BaseCurrency);
        var symbols = request.Symbols?
            .Select(CurrencyCode.Create)
            .ToArray();

        var provider = _providerFactory.Create(CurrencyProviderKind.Frankfurter);
        var providerResult = await provider.GetHistoricalRatesAsync(
            new HistoricalRatesProviderRequest(
                BaseCurrency: baseCurrency,
                StartDate: request.StartDate,
                EndDate: request.EndDate,
                Symbols: symbols),
            cancellationToken);

        var items = providerResult.Items
            .Select(snapshot => new HistoricalRateItemResponse(
                Date: snapshot.Date,
                Rates: snapshot.Rates
                    .OrderBy(pair => pair.Key.Value, StringComparer.Ordinal)
                    .ToDictionary(pair => pair.Key.Value, pair => pair.Value, StringComparer.Ordinal)))
            .ToArray();

        var totalItems = items.Length;
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)request.PageSize);

        var pagedItems = items
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArray();

        return new HistoricalRatesResponse(
            BaseCurrency: providerResult.BaseCurrency.Value,
            StartDate: providerResult.StartDate,
            EndDate: providerResult.EndDate,
            Sort: "date_desc",
            Page: new PagedResponse<HistoricalRateItemResponse>(
                Page: request.Page,
                PageSize: request.PageSize,
                TotalItems: totalItems,
                TotalPages: totalPages,
                Items: pagedItems));
    }
}
