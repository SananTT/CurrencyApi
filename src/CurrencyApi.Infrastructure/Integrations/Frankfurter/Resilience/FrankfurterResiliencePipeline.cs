using System.Net;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Configuration;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;
using Microsoft.Extensions.Logging;

namespace CurrencyApi.Infrastructure.Integrations.Frankfurter.Resilience;

public sealed class FrankfurterResiliencePipeline : IFrankfurterResiliencePipeline
{
    private readonly object _sync = new();
    private readonly IAsyncClock _clock;
    private readonly ILogger<FrankfurterResiliencePipeline> _logger;
    private readonly FrankfurterResilienceSettings _settings;
    private int _consecutiveFailures;
    private DateTimeOffset? _circuitOpenUntilUtc;

    public FrankfurterResiliencePipeline(
        FrankfurterResilienceSettings settings,
        IAsyncClock clock,
        ILogger<FrankfurterResiliencePipeline> logger)
    {
        _settings = settings;
        _clock = clock;
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        EnsureCircuitAllowsExecution();

        Exception? lastException = null;

        for (var attempt = 0; attempt <= _settings.MaxRetryAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(_settings.TimeoutMilliseconds));

                var result = await operation(timeoutCts.Token);
                ResetFailures();
                return result;
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                lastException = RegisterTransientFailure(new HttpRequestException(
                    "The upstream exchange rate provider request timed out.",
                    exception));
            }
            catch (HttpRequestException exception) when (IsTransient(exception))
            {
                lastException = RegisterTransientFailure(exception);
            }

            if (attempt == _settings.MaxRetryAttempts)
            {
                break;
            }

            var delay = TimeSpan.FromMilliseconds(_settings.BaseDelayMilliseconds * Math.Pow(2, attempt));
            _logger.LogWarning(
                "Frankfurter transient failure detected. Attempt={Attempt} DelayMs={DelayMs} ExceptionMessage={ExceptionMessage}",
                attempt + 1,
                delay.TotalMilliseconds,
                lastException.Message);
            await _clock.DelayAsync(delay, cancellationToken);

            EnsureCircuitAllowsExecution();
        }

        throw lastException ?? new HttpRequestException("The upstream exchange rate provider is currently unavailable.");
    }

    private Exception RegisterTransientFailure(HttpRequestException exception)
    {
        lock (_sync)
        {
            _consecutiveFailures++;
            if (_consecutiveFailures >= _settings.CircuitBreakerFailureThreshold)
            {
                _circuitOpenUntilUtc = _clock.UtcNow.AddSeconds(_settings.CircuitBreakerBreakSeconds);
                _logger.LogError(
                    "Frankfurter circuit opened. FailureCount={FailureCount} OpenUntilUtc={OpenUntilUtc}",
                    _consecutiveFailures,
                    _circuitOpenUntilUtc.Value);
            }
        }

        return exception;
    }

    private void ResetFailures()
    {
        lock (_sync)
        {
            _consecutiveFailures = 0;
            _circuitOpenUntilUtc = null;
        }
    }

    private void EnsureCircuitAllowsExecution()
    {
        lock (_sync)
        {
            if (_circuitOpenUntilUtc is null)
            {
                return;
            }

            if (_clock.UtcNow >= _circuitOpenUntilUtc.Value)
            {
                _logger.LogInformation("Frankfurter circuit closed and execution is allowed again.");
                _consecutiveFailures = 0;
                _circuitOpenUntilUtc = null;
                return;
            }
        }

        throw new HttpRequestException("The upstream exchange rate provider circuit is currently open.");
    }

    private static bool IsTransient(HttpRequestException exception)
    {
        if (!exception.StatusCode.HasValue)
        {
            return true;
        }

        return exception.StatusCode.Value is
            HttpStatusCode.RequestTimeout or
            HttpStatusCode.TooManyRequests or
            HttpStatusCode.BadGateway or
            HttpStatusCode.ServiceUnavailable or
            HttpStatusCode.GatewayTimeout ||
            (int)exception.StatusCode.Value >= 500;
    }
}
