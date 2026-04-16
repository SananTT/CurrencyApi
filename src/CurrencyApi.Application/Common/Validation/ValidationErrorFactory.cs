namespace CurrencyApi.Application.Common.Validation;

public static class ValidationErrorFactory
{
    public static ValidationError Required(string target) =>
        new(
            Code: "validation.required",
            Message: $"{target} is required.",
            Target: target);

    public static ValidationError InvalidCurrency(string target, string? value) =>
        new(
            Code: "validation.invalid_currency",
            Message: $"'{value ?? "<null>"}' is not a valid currency code for {target}.",
            Target: target);

    public static ValidationError ExcludedCurrency(string target, string currencyCode) =>
        new(
            Code: "validation.excluded_currency",
            Message: $"Currency '{currencyCode}' is not supported for {target}.",
            Target: target);

    public static ValidationError AmountMustBePositive(string target) =>
        new(
            Code: "validation.amount_must_be_positive",
            Message: $"{target} must be greater than zero.",
            Target: target);

    public static ValidationError InvalidDateRange(string target, string message) =>
        new(
            Code: "validation.date_range_invalid",
            Message: message,
            Target: target);

    public static ValidationError PageOutOfRange(string target) =>
        new(
            Code: "validation.page_out_of_range",
            Message: $"{target} must be greater than or equal to 1.",
            Target: target);

    public static ValidationError PageSizeOutOfRange(string target, int maxPageSize) =>
        new(
            Code: "validation.page_size_out_of_range",
            Message: $"{target} must be between 1 and {maxPageSize}.",
            Target: target);

    public static ValidationError DuplicateSymbol(string target, string currencyCode) =>
        new(
            Code: "validation.duplicate_symbol",
            Message: $"Currency '{currencyCode}' appears more than once in {target}.",
            Target: target);

    public static ValidationError InvalidProviderKind(string target, object? value) =>
        new(
            Code: "validation.invalid_provider_kind",
            Message: $"'{value ?? "<null>"}' is not a supported provider value for {target}.",
            Target: target);
}
