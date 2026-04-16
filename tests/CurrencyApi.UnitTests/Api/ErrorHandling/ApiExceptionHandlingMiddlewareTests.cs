using System.Text.Json;
using CurrencyApi.Api.ErrorHandling;
using CurrencyApi.Application.Common.Contracts;
using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Common.Validation;
using Microsoft.AspNetCore.Http;

namespace CurrencyApi.UnitTests.Api.ErrorHandling;

public sealed class ApiExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldWriteMappedJsonError_WhenUnhandledExceptionOccurs()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123",
        };
        context.Response.Body = new MemoryStream();

        var middleware = new ApiExceptionHandlingMiddleware(_ =>
            throw new RequestValidationException(
                [new ValidationError("validation.invalid_currency", "Invalid currency.", "BaseCurrency")]));

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var payload = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(context.Response.Body, options);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.Equal("application/json", context.Response.ContentType);
        Assert.NotNull(payload);
        Assert.Equal("validation.invalid_currency", payload!.Code);
        Assert.Equal("trace-123", payload.TraceId);
    }

    [Fact]
    public async Task InvokeAsync_ShouldRethrowOperationCanceled_WhenRequestWasAborted()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var context = new DefaultHttpContext();
        context.RequestAborted = cts.Token;

        var middleware = new ApiExceptionHandlingMiddleware(_ => throw new OperationCanceledException(cts.Token));

        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
    }
}
