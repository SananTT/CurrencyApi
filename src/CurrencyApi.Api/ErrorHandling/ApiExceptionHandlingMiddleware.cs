using System.Text.Json;
using CurrencyApi.Application.Common.Contracts;

namespace CurrencyApi.Api.ErrorHandling;

internal sealed class ApiExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RequestDelegate _next;

    public ApiExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (OperationCanceledException) when (httpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var mapped = ApiExceptionMapper.Map(exception);

            httpContext.Response.StatusCode = mapped.StatusCode;
            httpContext.Response.ContentType = "application/json";

            var payload = new ApiErrorResponse(
                Code: mapped.Code,
                Message: mapped.Message,
                TraceId: httpContext.TraceIdentifier);

            await JsonSerializer.SerializeAsync(
                httpContext.Response.Body,
                payload,
                JsonOptions,
                httpContext.RequestAborted);
        }
    }
}
