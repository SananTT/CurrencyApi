using CurrencyApi.Application.Auth.Contracts;
using CurrencyApi.Application.Auth.UseCases.Login;
using CurrencyApi.Application.Auth.Validation;
using CurrencyApi.Application.Common.Exceptions;

namespace CurrencyApi.UnitTests.Application.Auth.UseCases;

public sealed class LoginUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnLoginResponse_WhenCredentialsAreValid()
    {
        var useCase = new LoginUseCase(
            userAuthenticator: new StubUserAuthenticator(),
            accessTokenIssuer: new StubAccessTokenIssuer(),
            validator: new LoginRequestValidator());

        var result = await useCase.ExecuteAsync(new LoginRequest("viewer", "secret"));

        Assert.Equal("token-123", result.AccessToken);
        Assert.Equal("User", result.Role);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowRequestValidationException_WhenRequestIsInvalid()
    {
        var useCase = new LoginUseCase(
            userAuthenticator: new StubUserAuthenticator(),
            accessTokenIssuer: new StubAccessTokenIssuer(),
            validator: new LoginRequestValidator());

        await Assert.ThrowsAsync<RequestValidationException>(() => useCase.ExecuteAsync(new LoginRequest("", "")));
    }

    [Fact]
    public async Task ExecuteAsync_ShouldBubbleInvalidCredentials_WhenAuthenticatorRejectsUser()
    {
        var useCase = new LoginUseCase(
            userAuthenticator: new RejectingUserAuthenticator(),
            accessTokenIssuer: new StubAccessTokenIssuer(),
            validator: new LoginRequestValidator());

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => useCase.ExecuteAsync(new LoginRequest("viewer", "wrong")));
    }

    private sealed class StubUserAuthenticator : IUserAuthenticator
    {
        public Task<AuthenticatedUser> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AuthenticatedUser("viewer", username, "viewer-client", "User"));
        }
    }

    private sealed class RejectingUserAuthenticator : IUserAuthenticator
    {
        public Task<AuthenticatedUser> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            throw new InvalidCredentialsException();
        }
    }

    private sealed class StubAccessTokenIssuer : IAccessTokenIssuer
    {
        public AccessTokenResult IssueToken(AuthenticatedUser user) =>
            new("token-123", DateTimeOffset.UtcNow.AddHours(1));
    }
}
