using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Snapshooter.Xunit;
using Xunit.Priority;

namespace Nzr.Diagnostics.HealthChecks.Tests.Integration;

public class ThreadPoolHealthCheckTests : IDisposable
{
    private readonly ThreadPoolHealthCheck _healthCheck;
    private readonly CancellationTokenSource _cts;

    public ThreadPoolHealthCheckTests()
    {
        _cts = new CancellationTokenSource();
        _healthCheck = new ThreadPoolHealthCheck(new NullLogger<ThreadPoolHealthCheck>());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cts.Cancel();
            _cts.Dispose();
        }
    }

    [Fact, Priority(1)]
    public async Task HealthCheck_Should_Log_Thread_Pool_Status()
    {
        // Arrange

        ThreadPool.SetMinThreads(2, 2);
        ThreadPool.SetMaxThreads(8, 8);

        ThreadPool.GetMinThreads(out var minWorkerThreads, out var MinCompletionPortThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var MaxCompletionPortThreads);

        // Act

        var healthCheckResult = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert

        // Verify the collected values are as expected
        healthCheckResult.Data[ThreadPoolHealthCheck.MinWorkerThreadsDataKey].Should().Be(minWorkerThreads);
        healthCheckResult.Data[ThreadPoolHealthCheck.MinCompletionPortThreadsDataKey].Should().Be(MinCompletionPortThreads);
        healthCheckResult.Data[ThreadPoolHealthCheck.MaxWorkerThreadsDataKey].Should().Be(maxWorkerThreads);
        healthCheckResult.Data[ThreadPoolHealthCheck.MaxCompletionPortThreadsDataKey].Should().Be(MaxCompletionPortThreads);

        ThreadPool.SetMinThreads(minWorkerThreads, MinCompletionPortThreads);
        ThreadPool.SetMaxThreads(maxWorkerThreads, MaxCompletionPortThreads);
    }

    [Fact, Priority(2)]
    public async Task HealthCheck_Should_Detect_Thread_Starvation()
    {
        // Arrange

        ThreadPool.GetMinThreads(out var originalMinWorkerThreads, out var originalMinCompletionPortThreads);
        ThreadPool.GetMaxThreads(out var originalMaxWorkerThreads, out var originalMaxCompletionPortThreads);

        // Lower the min and max worker threads to cause starvation
        ThreadPool.SetMinThreads(1, 1);
        ThreadPool.SetMaxThreads(2, 2);

        // Create some pressure on the thread pool

        var tasks = new List<Task>();

        for (var i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                await Task.Delay(1000); // Simulate work
            }, _cts.Token));
        }

        // Act

        var healthCheckResult = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert

        healthCheckResult.Should().MatchSnapshot(options => options
            .IgnoreFields(nameof(HealthCheckResult.Data))
            .IgnoreField(nameof(HealthCheckResult.Description)));

        // Verify the collected values are as expected
        healthCheckResult.Data[ThreadPoolHealthCheck.MinWorkerThreadsDataKey].Should().Be(1);
        healthCheckResult.Data[ThreadPoolHealthCheck.MinCompletionPortThreadsDataKey].Should().Be(1);
        healthCheckResult.Data[ThreadPoolHealthCheck.MaxWorkerThreadsDataKey].Should().Be(2);
        healthCheckResult.Data[ThreadPoolHealthCheck.MaxCompletionPortThreadsDataKey].Should().Be(2);

        // Cleanup

        // Restore the original settings
        ThreadPool.SetMinThreads(originalMinWorkerThreads, originalMinCompletionPortThreads);
        ThreadPool.SetMaxThreads(originalMaxWorkerThreads, originalMaxCompletionPortThreads);
        await Task.WhenAll(tasks);
    }
}
