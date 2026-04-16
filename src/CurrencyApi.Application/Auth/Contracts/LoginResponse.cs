namespace CurrencyApi.Application.Auth.Contracts;

public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc,
    string Role);
