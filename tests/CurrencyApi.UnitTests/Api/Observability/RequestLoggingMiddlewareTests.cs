using System.Security.Claims;
using CurrencyApi.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CurrencyApi.UnitTests.Api.Observability;

public sealed class RequestLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldLogCompletedRequest_WithResolvedUserAndClient()
    {
        var logger = new ListLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                await Task.CompletedTask;
            },
            logger);

        var context = new DefaultHttpContext
        {
            TraceIdentifier = "corr-789",
        };
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/api/v1/rates/latest";
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "GetLatestRates"));
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("client_id", "viewer-client"),
                new Claim("unique_name", "viewer"),
            ],
            "Bearer"));

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.LogLevel);
        Assert.Contains("HTTP request completed.", entry.Message, StringComparison.Ordinal);
        Assert.Contains("viewer", entry.Message, StringComparison.Ordinal);
        Assert.Contains("viewer-client", entry.Message, StringComparison.Ordinal);
        Assert.Contains("corr-789", entry.Message, StringComparison.Ordinal);
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
        }

        public sealed record LogEntry(LogLevel LogLevel, string Message);

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
