using CurrencyApi.Application.Auth.Contracts;
using CurrencyApi.Application.Common.Exceptions;

namespace CurrencyApi.Api.Authentication;

public sealed class SeededUserAuthenticator : IUserAuthenticator
{
    private readonly IReadOnlyDictionary<string, SeededUserAccountSettings> _usersByUsername;

    public SeededUserAuthenticator(SeededAuthSettings settings)
    {
        _usersByUsername = settings.Users
            .Select(NormalizeUser)
            .ToDictionary(
            user => user.Username,
            user => user,
            StringComparer.OrdinalIgnoreCase);
    }

    public Task<AuthenticatedUser> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_usersByUsername.TryGetValue(username, out var user) ||
            !string.Equals(user.Password, password, StringComparison.Ordinal))
        {
            throw new InvalidCredentialsException();
        }

        return Task.FromResult(new AuthenticatedUser(
            UserId: user.Username,
            Username: user.Username,
            ClientId: string.IsNullOrWhiteSpace(user.ClientId) ? user.Username : user.ClientId,
            Role: user.Role));
    }

    private static SeededUserAccountSettings NormalizeUser(SeededUserAccountSettings user) =>
        new()
        {
            Username = user.Username,
            Password = user.Password,
            ClientId = user.ClientId,
            Role = SystemRoles.Normalize(user.Role),
        };
}
