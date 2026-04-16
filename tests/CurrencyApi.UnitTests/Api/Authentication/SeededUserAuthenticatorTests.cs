using CurrencyApi.Api.Authentication;
using CurrencyApi.Application.Auth.Contracts;
using CurrencyApi.Application.Common.Exceptions;

namespace CurrencyApi.UnitTests.Api.Authentication;

public sealed class SeededUserAuthenticatorTests
{
    [Fact]
    public async Task AuthenticateAsync_ShouldNormalizeConfiguredRole_WhenCredentialsAreValid()
    {
        var authenticator = new SeededUserAuthenticator(new SeededAuthSettings
        {
            Users =
            [
                new SeededUserAccountSettings
                {
                    Username = "admin",
                    Password = "secret",
                    Role = "admin",
                    ClientId = "ops-client",
                },
            ],
        });

        var result = await authenticator.AuthenticateAsync("admin", "secret");

        Assert.Equal(SystemRoles.Admin, result.Role);
        Assert.Equal("ops-client", result.ClientId);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldThrowInvalidCredentialsException_WhenPasswordIsWrong()
    {
        var authenticator = new SeededUserAuthenticator(new SeededAuthSettings
        {
            Users =
            [
                new SeededUserAccountSettings
                {
                    Username = "viewer",
                    Password = "secret",
                    Role = SystemRoles.User,
                },
            ],
        });

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            authenticator.AuthenticateAsync("viewer", "wrong"));
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidOperationException_WhenRoleIsUnsupported()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SeededUserAuthenticator(new SeededAuthSettings
            {
                Users =
                [
                    new SeededUserAccountSettings
                    {
                        Username = "ops",
                        Password = "secret",
                        Role = "Supervisor",
                    },
                ],
            }));
    }
}
