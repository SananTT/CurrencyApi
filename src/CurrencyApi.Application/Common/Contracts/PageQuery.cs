namespace CurrencyApi.Application.Common.Contracts;

public sealed record PageQuery(int Page = 1, int PageSize = 10);
