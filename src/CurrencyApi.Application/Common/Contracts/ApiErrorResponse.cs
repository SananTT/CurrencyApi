namespace CurrencyApi.Application.Common.Contracts;

public sealed record ApiErrorResponse(string Code, string Message, string? TraceId);
