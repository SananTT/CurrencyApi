namespace CurrencyApi.Application.Common.Contracts;

public sealed record PagedResponse<TItem>(
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages,
    IReadOnlyList<TItem> Items);
