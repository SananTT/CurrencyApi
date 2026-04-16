using System.Text;
using System.Security.Claims;
using System.Globalization;
using CurrencyApi.Api.Authorization;
using CurrencyApi.Api.Authentication;
using CurrencyApi.Api.Observability;
using CurrencyApi.Api.RateLimiting;
using CurrencyApi.Api.Swagger;
using CurrencyApi.Application.Admin.Contracts;
using CurrencyApi.Application.Admin.UseCases.ClearCache;
using CurrencyApi.Application.Admin.Validation;
using CurrencyApi.Application.Auth.Contracts;
using CurrencyApi.Application.Auth.UseCases.Login;
using CurrencyApi.Application.Auth.Validation;
using CurrencyApi.Application.Common.Validation;
using CurrencyApi.Application.Providers.Contracts;
using CurrencyApi.Application.Rates.Contracts;
using CurrencyApi.Application.Rates.UseCases.ConvertCurrency;
using CurrencyApi.Application.Rates.UseCases.GetHistoricalRates;
using CurrencyApi.Application.Rates.UseCases.GetLatestRates;
using CurrencyApi.Application.Rates.Validation;
using CurrencyApi.Infrastructure.Caching;
using CurrencyApi.Infrastructure.Caching.Configuration;
using CurrencyApi.Infrastructure.Integrations.Frankfurter;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Configuration;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Resilience;
using CurrencyApi.Infrastructure.Providers;
using CurrencyApi.Infrastructure.Providers.Frankfurter;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;
using System.Text.Json;
using CurrencyApi.Application.Common.Contracts;

namespace CurrencyApi.Api;

[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCurrencyApiServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection("Jwt")
            .Get<JwtAuthSettings>() ?? new JwtAuthSettings();

        var seededAuthSettings = configuration
            .GetSection("Authentication")
            .Get<SeededAuthSettings>() ?? new SeededAuthSettings();

        var frankfurterSettings = configuration
            .GetSection("Frankfurter")
            .Get<FrankfurterClientSettings>() ?? new FrankfurterClientSettings();

        var frankfurterResilienceSettings = configuration
            .GetSection("Frankfurter:Resilience")
            .Get<FrankfurterResilienceSettings>() ?? new FrankfurterResilienceSettings();

        var cacheSettings = configuration
            .GetSection("Cache")
            .Get<RatesCacheSettings>() ?? new RatesCacheSettings();

        var rateLimitingSettings = configuration
            .GetSection("RateLimiting")
            .Get<RateLimitingSettings>() ?? new RateLimitingSettings();

        services.AddSingleton(jwtSettings);
        services.AddSingleton(seededAuthSettings);
        services.AddSingleton(frankfurterSettings);
        services.AddSingleton(frankfurterResilienceSettings);
        services.AddSingleton(cacheSettings);
        services.AddSingleton(rateLimitingSettings);

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddHealthChecks();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Currency API",
                Version = "v1",
                Description = "Exchange rates API with authentication, admin operations, rate limiting, and observability.",
            });

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Provide a valid JWT access token.",
            });

            options.OperationFilter<SwaggerAuthorizeOperationFilter>();
        });
        services.AddSingleton<IClientIdentityResolver, HttpClientIdentityResolver>();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("RateLimiting");

                logger.LogWarning(
                    "Rate limit exceeded. Path={Path} CorrelationId={CorrelationId}",
                    context.HttpContext.Request.Path.Value ?? "/",
                    context.HttpContext.TraceIdentifier);

                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.Headers[CorrelationHeaders.CorrelationId] = context.HttpContext.TraceIdentifier;

                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
                }

                var payload = new ApiErrorResponse(
                    Code: "rate_limit.exceeded",
                    Message: "Rate limit exceeded. Please try again later.",
                    TraceId: context.HttpContext.TraceIdentifier);

                await JsonSerializer.SerializeAsync(
                    context.HttpContext.Response.Body,
                    payload,
                    cancellationToken: cancellationToken);
            };

            options.AddPolicy(RateLimitPolicies.Login, httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<RateLimitingSettings>();
                var resolver = httpContext.RequestServices.GetRequiredService<IClientIdentityResolver>();

                return RateLimitPartition.GetFixedWindowLimiter(
                    resolver.ResolveLoginPartition(httpContext),
                    _ => BuildFixedWindowOptions(settings.Login));
            });

            options.AddPolicy(RateLimitPolicies.Historical, httpContext =>
            {
                var settings = httpContext.RequestServices.GetRequiredService<RateLimitingSettings>();
                var resolver = httpContext.RequestServices.GetRequiredService<IClientIdentityResolver>();

                return RateLimitPartition.GetFixedWindowLimiter(
                    resolver.ResolveAuthenticatedClientPartition(httpContext),
                    _ => BuildFixedWindowOptions(settings.Historical));
            });
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
                    RoleClaimType = ClaimTypes.Role,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                AuthorizationPolicies.UserAccess,
                policy => policy.RequireRole(SystemRoles.User, SystemRoles.Admin));

            options.AddPolicy(
                AuthorizationPolicies.AdminOnly,
                policy => policy.RequireRole(SystemRoles.Admin));
        });

        services.AddHttpClient<IFrankfurterApiClient, FrankfurterApiClient>(client =>
        {
            client.BaseAddress = new Uri(frankfurterSettings.BaseUrl, UriKind.Absolute);
        })
        .AddHttpMessageHandler<OutboundCorrelationHandler>();

        services.AddSingleton<IValidator<LoginRequest>, LoginRequestValidator>();
        services.AddSingleton<IValidator<ClearCacheRequest>, ClearCacheRequestValidator>();
        services.AddSingleton<IValidator<LatestRatesRequest>, LatestRatesRequestValidator>();
        services.AddSingleton<IValidator<ConvertCurrencyRequest>, ConvertCurrencyRequestValidator>();
        services.AddSingleton<IValidator<HistoricalRatesRequest>, HistoricalRatesRequestValidator>();
        services.AddTransient<ILoginUseCase, LoginUseCase>();
        services.AddTransient<IClearCacheUseCase, ClearCacheUseCase>();
        services.AddTransient<IGetLatestRatesUseCase, GetLatestRatesUseCase>();
        services.AddTransient<IConvertCurrencyUseCase, ConvertCurrencyUseCase>();
        services.AddTransient<IGetHistoricalRatesUseCase, GetHistoricalRatesUseCase>();
        services.AddSingleton<RatesCacheKeyRegistry>();
        services.AddSingleton<IRatesCacheInvalidator, RatesCacheInvalidator>();
        services.AddSingleton<ICorrelationContextAccessor, HttpCorrelationContextAccessor>();
        services.AddSingleton<IUserAuthenticator, SeededUserAuthenticator>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();
        services.AddSingleton<IAsyncClock, SystemAsyncClock>();
        services.AddSingleton<IFrankfurterResiliencePipeline, FrankfurterResiliencePipeline>();
        services.AddTransient<OutboundCorrelationHandler>();
        services.AddTransient<FrankfurterCurrencyProvider>();
        services.AddTransient<ICurrencyRatesProvider>(serviceProvider =>
            new CachedCurrencyRatesProvider(
                innerProvider: serviceProvider.GetRequiredService<FrankfurterCurrencyProvider>(),
                memoryCache: serviceProvider.GetRequiredService<IMemoryCache>(),
                keyRegistry: serviceProvider.GetRequiredService<RatesCacheKeyRegistry>(),
                settings: serviceProvider.GetRequiredService<RatesCacheSettings>()));
        services.AddTransient<ICurrencyProviderFactory, CurrencyProviderFactory>();

        return services;
    }

    private static FixedWindowRateLimiterOptions BuildFixedWindowOptions(EndpointRateLimitSettings settings) =>
        new()
        {
            PermitLimit = settings.PermitLimit,
            Window = TimeSpan.FromSeconds(settings.WindowSeconds),
            QueueLimit = settings.QueueLimit,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            AutoReplenishment = true,
        };
}
