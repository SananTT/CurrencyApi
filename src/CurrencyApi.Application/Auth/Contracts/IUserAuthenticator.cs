namespace CurrencyApi.Application.Auth.Contracts;

public interface IUserAuthenticator
{
    Task<AuthenticatedUser> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);
}
