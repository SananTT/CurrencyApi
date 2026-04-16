using CurrencyApi.Application.Common.Contracts;

namespace CurrencyApi.Application.Rates.Contracts;

public sealed record HistoricalRatesResponse(
    string BaseCurrency,
    DateOnly StartDate,
    DateOnly EndDate,
    string Sort,
    PagedResponse<HistoricalRateItemResponse> Page);
