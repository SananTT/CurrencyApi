namespace CurrencyApi.Api.Versioning;

public sealed record ApiVersionResponse(
    string DefaultVersion,
    IReadOnlyList<string> SupportedVersions);
