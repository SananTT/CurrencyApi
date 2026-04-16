using CurrencyApi.Application.Auth.Contracts;
using CurrencyApi.Application.Auth.Validation;

namespace CurrencyApi.UnitTests.Application.Auth.Validation;

public sealed class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public void Validate_ShouldReturnErrors_WhenUsernameAndPasswordAreMissing()
    {
        var result = _validator.Validate(new LoginRequest("", " "));

        Assert.False(result.IsValid);
        Assert.Collection(
            result.Errors,
            error => Assert.Equal(nameof(LoginRequest.Username), error.Target),
            error => Assert.Equal(nameof(LoginRequest.Password), error.Target));
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenCredentialsAreProvided()
    {
        var result = _validator.Validate(new LoginRequest("viewer", "secret"));

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}
