using CurrencyApi.Api;

namespace CurrencyApi.UnitTests.Api;

public sealed class RouteParsingTests
{
    [Fact]
    public void ParseSymbols_ShouldReturnNull_WhenInputIsMissing()
    {
        Assert.Null(RouteParsing.ParseSymbols(null));
        Assert.Null(RouteParsing.ParseSymbols(""));
        Assert.Null(RouteParsing.ParseSymbols("   "));
    }

    [Fact]
    public void ParseSymbols_ShouldTrimAndRemoveEmptyEntries_WhenInputContainsNoise()
    {
        var result = RouteParsing.ParseSymbols(" USD, ,GBP ,JPY ,, ");

        Assert.Equal(new[] { "USD", "GBP", "JPY" }, result);
    }
}
