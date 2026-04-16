using CurrencyApi.Domain.Common;
using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.UnitTests.Domain.Currencies;

public sealed class CurrencyCodeTests
{
    [Fact]
    public void Create_ShouldNormalize_TrimmedAndLowercaseInput()
    {
        var currencyCode = CurrencyCode.Create(" eur ");

        Assert.Equal("EUR", currencyCode.Value);
        Assert.Equal("EUR", currencyCode.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData("AZ-")]
    public void TryCreate_ShouldReject_InvalidCodes(string? input)
    {
        var success = CurrencyCode.TryCreate(input, out var currencyCode, out var error);

        Assert.False(success);
        Assert.Equal(default, currencyCode);
        Assert.NotNull(error);
        Assert.Equal("currency.invalid_code", error!.Code);
    }

    [Fact]
    public void Create_ShouldThrowDomainValidationException_ForInvalidCode()
    {
        var exception = Assert.Throws<DomainValidationException>(() => CurrencyCode.Create("EURO"));

        Assert.Equal("currency.invalid_code", exception.Error.Code);
        Assert.Contains("invalid", exception.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("TRY")]
    [InlineData("pln")]
    [InlineData(" thb ")]
    [InlineData("MXN")]
    public void IsExcluded_ShouldReturnTrue_ForBlockedCurrencies(string input)
    {
        var currencyCode = CurrencyCode.Create(input);

        Assert.True(currencyCode.IsExcluded);
        Assert.True(ExcludedCurrencyPolicy.IsExcluded(input));
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("usd")]
    [InlineData(" AZN ")]
    public void IsExcluded_ShouldReturnFalse_ForAllowedCurrencies(string input)
    {
        var currencyCode = CurrencyCode.Create(input);

        Assert.False(currencyCode.IsExcluded);
        Assert.False(ExcludedCurrencyPolicy.IsExcluded(input));
    }
}
