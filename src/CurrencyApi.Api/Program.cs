using CurrencyApi.Api;
using CurrencyApi.Api.Authorization;
using CurrencyApi.Api.ErrorHandling;
using CurrencyApi.Api.Observability;
using CurrencyApi.Api.RateLimiting;
using CurrencyApi.Api.Versioning;
using CurrencyApi.Application.Admin.Contracts;
using CurrencyApi.Application.Admin.UseCases.ClearCache;
using CurrencyApi.Application.Auth.Contracts;
using CurrencyApi.Application.Auth.UseCases.Login;
using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Application.Rates.UseCases.ConvertCurrency;
using CurrencyApi.Application.Rates.UseCases.GetHistoricalRates;
using CurrencyApi.Application.Rates.UseCases.GetLatestRates;
using Serilog;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext());

    builder.Services.AddCurrencyApiServices(builder.Configuration);

    var app = builder.Build();

    app.UseMiddleware<RequestCorrelationMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();
    app.UseMiddleware<ApiExceptionHandlingMiddleware>();
    app.UseRouting();
    app.UseAuthentication();
    app.UseRateLimiter();
    app.UseAuthorization();

    if (!app.Environment.IsProduction())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Currency API v1");
            options.RoutePrefix = "swagger";
        });
    }

    app.MapGet("/", () => Results.Redirect("/swagger"));

    app.MapHealthChecks("/health")
        .WithTags("Platform")
        .WithName("HealthCheck")
        .AllowAnonymous();

    app.MapGet("/api/versions", () => Results.Ok(new ApiVersionResponse(
            DefaultVersion: ApiVersions.V1,
            SupportedVersions: ApiVersions.Supported)))
        .WithTags("Platform")
        .WithName("ApiVersions")
        .AllowAnonymous();

    var apiV1 = app.MapGroup($"/api/{ApiVersions.V1}")
        .WithTags("Api");

    var auth = apiV1.MapGroup("/auth")
        .WithTags("Auth");

    auth.MapPost("/login", async Task<IResult> (
        LoginRequest request,
        ILoginUseCase useCase,
        CancellationToken cancellationToken) =>
    {
        var response = await useCase.ExecuteAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("Login")
    .RequireRateLimiting(RateLimitPolicies.Login);

    var rates = apiV1.MapGroup("/rates")
        .WithTags("Rates")
        .RequireAuthorization(AuthorizationPolicies.UserAccess);

    rates.MapGet("/latest", async Task<IResult> (
        string @base,
        IGetLatestRatesUseCase useCase,
        CancellationToken cancellationToken) =>
    {
        var response = await useCase.ExecuteAsync(new LatestRatesRequest(@base), cancellationToken);
        return Results.Ok(response);
    })
    .WithName("GetLatestRates");

    rates.MapGet("/convert", async Task<IResult> (
        string from,
        string to,
        decimal amount,
        IConvertCurrencyUseCase useCase,
        CancellationToken cancellationToken) =>
    {
        var response = await useCase.ExecuteAsync(
            new ConvertCurrencyRequest(from, to, amount),
            cancellationToken);

        return Results.Ok(response);
    })
    .WithName("ConvertCurrency");

    rates.MapGet("/historical", async Task<IResult> (
        string @base,
        DateOnly start,
        DateOnly end,
        int? page,
        int? pageSize,
        string? symbols,
        IGetHistoricalRatesUseCase useCase,
        CancellationToken cancellationToken) =>
    {
        var response = await useCase.ExecuteAsync(
            new HistoricalRatesRequest(
                BaseCurrency: @base,
                StartDate: start,
                EndDate: end,
                Page: page ?? 1,
                PageSize: pageSize ?? 10,
                Symbols: RouteParsing.ParseSymbols(symbols)),
            cancellationToken);

        return Results.Ok(response);
    })
    .WithName("GetHistoricalRates")
    .RequireRateLimiting(RateLimitPolicies.Historical);

    var admin = apiV1.MapGroup("/admin")
        .WithTags("Admin")
        .RequireAuthorization(AuthorizationPolicies.AdminOnly);

    admin.MapPost("/cache/clear", async Task<IResult> (
        ClearCacheRequest request,
        IClearCacheUseCase useCase,
        CancellationToken cancellationToken) =>
    {
        var response = await useCase.ExecuteAsync(request, cancellationToken);
        return Results.Ok(response);
    })
    .WithName("ClearCache");

    app.Run();
}
finally
{
    Log.CloseAndFlush();
}

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public partial class Program;
