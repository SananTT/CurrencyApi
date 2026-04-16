using CurrencyApi.Api.ErrorHandling;
using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Common.Validation;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Exceptions;

namespace CurrencyApi.UnitTests.Api.ErrorHandling;

public sealed class ApiExceptionMapperTests
{
    [Fact]
    public void Map_ShouldReturnBadRequest_ForRequestValidationException()
    {
        var exception = new RequestValidationException(
            [
                new ValidationError("validation.invalid_currency", "Invalid currency.", "BaseCurrency"),
            ]);

        var result = ApiExceptionMapper.Map(exception);

        Assert.Equal(400, result.StatusCode);
        Assert.Equal("validation.invalid_currency", result.Code);
        Assert.Equal("Invalid currency.", result.Message);
    }

    [Fact]
    public void Map_ShouldReturnBadGateway_ForFrankfurterContractException()
    {
        var result = ApiExceptionMapper.Map(new FrankfurterContractException("Payload mismatch."));

        Assert.Equal(502, result.StatusCode);
        Assert.Equal("upstream.invalid_response", result.Code);
        Assert.Equal("Payload mismatch.", result.Message);
    }

    [Fact]
    public void Map_ShouldReturnBadGateway_ForFrankfurterUpstreamException()
    {
        var result = ApiExceptionMapper.Map(new FrankfurterUpstreamException(System.Net.HttpStatusCode.BadRequest, "Upstream rejected request."));

        Assert.Equal(502, result.StatusCode);
        Assert.Equal("upstream.request_failed", result.Code);
        Assert.Equal("Upstream rejected request.", result.Message);
    }

    [Fact]
    public void Map_ShouldReturnUnauthorized_ForInvalidCredentialsException()
    {
        var result = ApiExceptionMapper.Map(new InvalidCredentialsException());

        Assert.Equal(401, result.StatusCode);
        Assert.Equal("auth.invalid_credentials", result.Code);
    }

    [Fact]
    public void Map_ShouldReturnServiceUnavailable_ForHttpRequestException()
    {
        var result = ApiExceptionMapper.Map(new HttpRequestException("No route to host."));

        Assert.Equal(503, result.StatusCode);
        Assert.Equal("upstream.unavailable", result.Code);
    }

    [Fact]
    public void Map_ShouldReturnInternalServerError_ForUnexpectedExceptions()
    {
        var result = ApiExceptionMapper.Map(new Exception("Boom"));

        Assert.Equal(500, result.StatusCode);
        Assert.Equal("server.unexpected_error", result.Code);
    }
}
