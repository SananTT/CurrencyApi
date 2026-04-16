using CurrencyApi.Application.Auth.Contracts;
using CurrencyApi.Application.Common.Exceptions;
using CurrencyApi.Application.Common.Validation;

namespace CurrencyApi.Application.Auth.UseCases.Login;

public sealed class LoginUseCase : ILoginUseCase
{
    private readonly IUserAuthenticator _userAuthenticator;
    private readonly IAccessTokenIssuer _accessTokenIssuer;
    private readonly IValidator<LoginRequest> _validator;

    public LoginUseCase(
        IUserAuthenticator userAuthenticator,
        IAccessTokenIssuer accessTokenIssuer,
        IValidator<LoginRequest> validator)
    {
        _userAuthenticator = userAuthenticator;
        _accessTokenIssuer = accessTokenIssuer;
        _validator = validator;
    }

    public async Task<LoginResponse> ExecuteAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            throw new RequestValidationException(validationResult.Errors);
        }

        var authenticatedUser = await _userAuthenticator.AuthenticateAsync(
            request.Username,
            request.Password,
            cancellationToken);

        var token = _accessTokenIssuer.IssueToken(authenticatedUser);

        return new LoginResponse(
            AccessToken: token.AccessToken,
            ExpiresAtUtc: token.ExpiresAtUtc,
            Role: authenticatedUser.Role);
    }
}
