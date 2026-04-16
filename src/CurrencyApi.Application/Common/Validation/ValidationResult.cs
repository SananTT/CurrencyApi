namespace CurrencyApi.Application.Common.Validation;

public sealed class ValidationResult
{
    public static ValidationResult Success { get; } = new([]);

    public ValidationResult(IReadOnlyList<ValidationError> errors)
    {
        Errors = errors;
    }

    public IReadOnlyList<ValidationError> Errors { get; }

    public bool IsValid => Errors.Count == 0;

    public static ValidationResult WithErrors(IEnumerable<ValidationError> errors) =>
        new(errors.ToArray());
}
