namespace CurrencyApi.Application.Auth.Contracts;

public sealed record AuthenticatedUser(
    string UserId,
    string Username,
    string ClientId,
    string Role);
