using CurrencyApi.Application.Admin.Contracts;
using CurrencyApi.Application.Admin.Validation;
using CurrencyApi.Application.Providers.Contracts;

namespace CurrencyApi.UnitTests.Application.Admin.Validation;

public sealed class ClearCacheRequestValidatorTests
{
    private readonly ClearCacheRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenProviderKindIsMissing()
    {
        var result = _validator.Validate(new ClearCacheRequest());

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenProviderKindIsSupported()
    {
        var result = _validator.Validate(new ClearCacheRequest(CurrencyProviderKind.Frankfurter));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Validate_ShouldReject_WhenProviderKindIsOutsideEnum()
    {
        var result = _validator.Validate(new ClearCacheRequest((CurrencyProviderKind)999));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Code == "validation.invalid_provider_kind");
    }
}
