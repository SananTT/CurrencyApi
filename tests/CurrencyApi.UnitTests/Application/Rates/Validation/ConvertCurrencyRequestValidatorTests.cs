using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Application.Rates.Validation;

namespace CurrencyApi.UnitTests.Application.Rates.Validation;

public sealed class ConvertCurrencyRequestValidatorTests
{
    private readonly ConvertCurrencyRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldReject_NonPositiveAmount()
    {
        var result = _validator.Validate(new ConvertCurrencyRequest("USD", "AZN", 0));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.amount_must_be_positive");
    }

    [Theory]
    [InlineData("TRY", "USD", nameof(ConvertCurrencyRequest.FromCurrency))]
    [InlineData("USD", "pln", nameof(ConvertCurrencyRequest.ToCurrency))]
    public void Validate_ShouldReject_ExcludedCurrencies(string fromCurrency, string toCurrency, string target)
    {
        var result = _validator.Validate(new ConvertCurrencyRequest(fromCurrency, toCurrency, 12.5m));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.excluded_currency" && error.Target == target);
    }

    [Fact]
    public void Validate_ShouldSucceed_ForAllowedCurrenciesAndPositiveAmount()
    {
        var result = _validator.Validate(new ConvertCurrencyRequest("USD", "AZN", 12.5m));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
