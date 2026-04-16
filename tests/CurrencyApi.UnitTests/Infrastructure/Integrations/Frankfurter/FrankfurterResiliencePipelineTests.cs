using System.Net;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Configuration;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Contracts;
using CurrencyApi.Infrastructure.Integrations.Frankfurter.Resilience;
using Microsoft.Extensions.Logging.Abstractions;

namespace CurrencyApi.UnitTests.Infrastructure.Integrations.Frankfurter;

public sealed class FrankfurterResiliencePipelineTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldRetryTransientFailure_AndEventuallySucceed()
    {
        var clock = new FakeAsyncClock();
        var pipeline = CreatePipeline(
            clock,
            new FrankfurterResilienceSettings
            {
                TimeoutMilliseconds = 1000,
                MaxRetryAttempts = 2,
                BaseDelayMilliseconds = 100,
                CircuitBreakerFailureThreshold = 5,
                CircuitBreakerBreakSeconds = 30,
            });

        var invocationCount = 0;

        var result = await pipeline.ExecuteAsync<int>(_ =>
        {
            invocationCount++;

            if (invocationCount < 3)
            {
                throw new HttpRequestException("Transient", null, HttpStatusCode.ServiceUnavailable);
            }

            return Task.FromResult(7);
        });

        Assert.Equal(7, result);
        Assert.Equal(3, invocationCount);
        Assert.Equal(
            new[] { TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200) },
            clock.RecordedDelays);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldOpenCircuit_AfterConfiguredFailureThreshold()
    {
        var clock = new FakeAsyncClock();
        var pipeline = CreatePipeline(
            clock,
            new FrankfurterResilienceSettings
            {
                TimeoutMilliseconds = 1000,
                MaxRetryAttempts = 0,
                BaseDelayMilliseconds = 50,
                CircuitBreakerFailureThreshold = 2,
                CircuitBreakerBreakSeconds = 30,
            });

        var invocationCount = 0;

        await Assert.ThrowsAsync<HttpRequestException>(() => pipeline.ExecuteAsync<int>(_ =>
        {
            invocationCount++;
            throw new HttpRequestException("Transient", null, HttpStatusCode.BadGateway);
        }));

        await Assert.ThrowsAsync<HttpRequestException>(() => pipeline.ExecuteAsync<int>(_ =>
        {
            invocationCount++;
            throw new HttpRequestException("Transient", null, HttpStatusCode.BadGateway);
        }));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => pipeline.ExecuteAsync<int>(_ =>
        {
            invocationCount++;
            return Task.FromResult(99);
        }));

        Assert.Contains("circuit", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, invocationCount);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldThrowHttpRequestException_WhenAttemptTimesOut()
    {
        var pipeline = CreatePipeline(
            new FakeAsyncClock(),
            new FrankfurterResilienceSettings
            {
                TimeoutMilliseconds = 25,
                MaxRetryAttempts = 0,
                BaseDelayMilliseconds = 10,
                CircuitBreakerFailureThreshold = 3,
                CircuitBreakerBreakSeconds = 30,
            });

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => pipeline.ExecuteAsync<int>(async cancellationToken =>
        {
            await Task.Delay(250, cancellationToken);
            return 1;
        }));

        Assert.Contains("timed out", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static FrankfurterResiliencePipeline CreatePipeline(
        IAsyncClock clock,
        FrankfurterResilienceSettings settings) =>
        new(settings, clock, NullLogger<FrankfurterResiliencePipeline>.Instance);

    private sealed class FakeAsyncClock : IAsyncClock
    {
        public DateTimeOffset UtcNow { get; private set; } = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public List<TimeSpan> RecordedDelays { get; } = [];

        public Task DelayAsync(TimeSpan delay, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RecordedDelays.Add(delay);
            UtcNow = UtcNow.Add(delay);
            return Task.CompletedTask;
        }
    }
}
