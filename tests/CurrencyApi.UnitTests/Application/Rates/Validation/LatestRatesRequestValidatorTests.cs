using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Application.Rates.Validation;

namespace CurrencyApi.UnitTests.Application.Rates.Validation;

public sealed class LatestRatesRequestValidatorTests
{
    private readonly LatestRatesRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldReject_InvalidBaseCurrency()
    {
        var result = _validator.Validate(new LatestRatesRequest("EURO"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.invalid_currency");
    }

    [Fact]
    public void Validate_ShouldSucceed_ForValidBaseCurrency()
    {
        var result = _validator.Validate(new LatestRatesRequest("EUR"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
