using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Application.Rates.Validation;

namespace CurrencyApi.UnitTests.Application.Rates.Validation;

public sealed class HistoricalRatesRequestValidatorTests
{
    private readonly HistoricalRatesRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldReject_WhenDateRangeIsReversed()
    {
        var request = new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: new DateOnly(2024, 2, 10),
            EndDate: new DateOnly(2024, 2, 1));

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.date_range_invalid");
    }

    [Fact]
    public void Validate_ShouldReject_WhenDatesAreMissing()
    {
        var request = new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: default,
            EndDate: default);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.date_range_invalid");
    }

    [Fact]
    public void Validate_ShouldReject_WhenPageIsBelowOne()
    {
        var request = new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 5),
            Page: 0);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.page_out_of_range");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(51)]
    public void Validate_ShouldReject_WhenPageSizeIsOutsideAllowedRange(int pageSize)
    {
        var request = new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 5),
            PageSize: pageSize);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.page_size_out_of_range");
    }

    [Fact]
    public void Validate_ShouldReject_WhenSymbolsContainInvalidCurrency()
    {
        var request = new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 5),
            Symbols: ["USD", "EURO"]);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.invalid_currency");
    }

    [Fact]
    public void Validate_ShouldReject_WhenSymbolsContainExcludedCurrency()
    {
        var request = new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 5),
            Symbols: ["USD", "THB"]);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.excluded_currency");
    }

    [Fact]
    public void Validate_ShouldReject_WhenSymbolsContainDuplicates()
    {
        var request = new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 5),
            Symbols: ["USD", "usd"]);

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.duplicate_symbol");
    }

    [Fact]
    public void Validate_ShouldSucceed_ForWellFormedRequest()
    {
        var request = new HistoricalRatesRequest(
            BaseCurrency: "EUR",
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: new DateOnly(2024, 1, 5),
            Page: 2,
            PageSize: 20,
            Symbols: ["USD", "GBP"]);

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
