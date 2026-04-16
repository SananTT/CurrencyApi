namespace CurrencyApi.Application.Auth.Contracts;

public sealed record AccessTokenResult(
    string AccessToken,
    DateTimeOffset ExpiresAtUtc);
