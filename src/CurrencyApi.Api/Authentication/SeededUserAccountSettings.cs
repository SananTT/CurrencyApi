namespace CurrencyApi.Api.Authentication;

public sealed class SeededUserAccountSettings
{
    public string Username { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;
}
