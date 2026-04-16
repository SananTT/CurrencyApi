namespace CurrencyApi.Api.Versioning;

public static class ApiVersions
{
    public const string V1 = "v1";

    public static readonly IReadOnlyList<string> Supported = [V1];
}
