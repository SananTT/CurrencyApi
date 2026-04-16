namespace CurrencyApi.Api.Authentication;

public sealed class SeededAuthSettings
{
    public IReadOnlyList<SeededUserAccountSettings> Users { get; init; } = [];
}
