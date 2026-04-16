namespace CurrencyApi.Application.Rates.Contracts;

public sealed record HistoricalRatesRequest(
    string BaseCurrency,
    DateOnly StartDate,
    DateOnly EndDate,
    int Page = 1,
    int PageSize = 10,
    IReadOnlyList<string>? Symbols = null);
