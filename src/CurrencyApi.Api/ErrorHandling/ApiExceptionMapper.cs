using System.Net;
using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Exceptions;

namespace CurrencyApi.Api.ErrorHandling;

public static class ApiExceptionMapper
{
    public static ExceptionHandlingResult Map(Exception exception)
    {
        return exception switch
        {
            RequestValidationException validationException => MapValidation(validationException),
            InvalidCredentialsException invalidCredentialsException => new ExceptionHandlingResult(
                StatusCode: (int)HttpStatusCode.Unauthorized,
                Code: "auth.invalid_credentials",
                Message: invalidCredentialsException.Message),
            FrankfurterContractException frankfurterContractException => new ExceptionHandlingResult(
                StatusCode: (int)HttpStatusCode.BadGateway,
                Code: "upstream.invalid_response",
                Message: frankfurterContractException.Message),
            FrankfurterUpstreamException frankfurterUpstreamException => new ExceptionHandlingResult(
                StatusCode: (int)HttpStatusCode.BadGateway,
                Code: "upstream.request_failed",
                Message: frankfurterUpstreamException.Message),
            HttpRequestException => new ExceptionHandlingResult(
                StatusCode: (int)HttpStatusCode.ServiceUnavailable,
                Code: "upstream.unavailable",
                Message: "The upstream exchange rate provider is currently unavailable."),
            _ => new ExceptionHandlingResult(
                StatusCode: (int)HttpStatusCode.InternalServerError,
                Code: "server.unexpected_error",
                Message: "An unexpected error occurred."),
        };
    }

    private static ExceptionHandlingResult MapValidation(RequestValidationException exception)
    {
        if (exception.Errors.Count == 0)
        {
            return new ExceptionHandlingResult(
                StatusCode: (int)HttpStatusCode.BadRequest,
                Code: "validation.invalid_request",
                Message: "The request is invalid.");
        }

        var firstError = exception.Errors[0];

        return new ExceptionHandlingResult(
            StatusCode: (int)HttpStatusCode.BadRequest,
            Code: firstError.Code,
            Message: firstError.Message);
    }
}
