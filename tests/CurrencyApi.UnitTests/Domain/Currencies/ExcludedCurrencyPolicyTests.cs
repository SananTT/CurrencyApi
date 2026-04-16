using CurrencyApi.Domain.Currencies;

namespace CurrencyApi.UnitTests.Domain.Currencies;

public sealed class ExcludedCurrencyPolicyTests
{
    [Fact]
    public void Codes_ShouldExposeExpectedBlockedCurrencySet()
    {
        var codes = ExcludedCurrencyPolicy.Codes;

        Assert.Equal(4, codes.Count);
        Assert.Contains("TRY", codes);
        Assert.Contains("PLN", codes);
        Assert.Contains("THB", codes);
        Assert.Contains("MXN", codes);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("US")]
    [InlineData("123")]
    public void IsExcluded_ShouldReturnFalse_ForInvalidValues(string? input)
    {
        Assert.False(ExcludedCurrencyPolicy.IsExcluded(input));
    }
}
