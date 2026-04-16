namespace CurrencyApi.Api.ErrorHandling;

public sealed record ExceptionHandlingResult(int StatusCode, string Code, string Message);
