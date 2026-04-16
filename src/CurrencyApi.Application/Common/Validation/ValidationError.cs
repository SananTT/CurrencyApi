namespace CurrencyApi.Application.Common.Validation;

public sealed record ValidationError(string Code, string Message, string? Target = null);
