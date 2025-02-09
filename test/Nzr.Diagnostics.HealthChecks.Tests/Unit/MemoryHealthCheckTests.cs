using System.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using static Nzr.Diagnostics.HealthChecks.MemoryHealthCheck;

namespace Nzr.Diagnostics.HealthChecks.Tests.Unit;

public class MemoryHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_When_Memory_Below_Thresholds_Should_Return_Healthy()
    {
        // Arrange

        var healthCheck = CreateMemoryHealthCheck(600, 800);

        // Act

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert

        result.Status.Should().Be(HealthStatus.Healthy);

        result.Description.Should().Be("Memory usage is within normal range. Allocated: 600MB, Working Set: 800MB");
    }

    [Fact]
    public async Task CheckHealthAsync_When_Memory_Above_Warning_Threshold_Should_Return_Degraded()
    {
        // Arrange

        var healthCheck = CreateMemoryHealthCheck(900, 1600);

        // Act

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert

        result.Status.Should().Be(HealthStatus.Degraded);
        result.Description.Should().Be("Memory usage is approaching critical levels! Allocated: 900MB, Working Set: 1600MB");
    }

    [Fact]
    public async Task CheckHealthAsync_When_Memory_Above_Critical_Threshold_Should_Return_Unhealthy()
    {
        // Arrange

        var healthCheck = CreateMemoryHealthCheck(1500, 2200);

        // Act

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Be("Memory usage exceeds critical threshold! Allocated: 1500MB, Working Set: 2200MB");
    }

    private class TestMemoryHealthCheck : MemoryHealthCheck
    {
        private readonly MemoryMetrics _memoryMetrics;

        public TestMemoryHealthCheck(IOptionsMonitor<MemoryHealthCheckOptions> options, ILogger<MemoryHealthCheck> logger, MemoryMetrics memoryMetrics)
            : base(options, logger)
        {
            _memoryMetrics = memoryMetrics;
        }

        protected override Task<MemoryMetrics> CollectMemoryMetricsAsync(Process currentProcess)
        {
            return Task.FromResult(_memoryMetrics);
        }
    }

    private static IOptionsMonitor<MemoryHealthCheckOptions> CreateOptions(
        long warningThreshold,
        long criticalThreshold,
        long workingSetWarningThreshold,
        long workingSetCriticalThreshold)
    {
        var options = new MemoryHealthCheckOptions
        {
            WarningThreshold = warningThreshold,
            CriticalThreshold = criticalThreshold,
            WorkingSetWarningThreshold = workingSetWarningThreshold,
            WorkingSetCriticalThreshold = workingSetCriticalThreshold
        };

        var optionsMonitor = Substitute.For<IOptionsMonitor<MemoryHealthCheckOptions>>();
        optionsMonitor.CurrentValue.Returns(options);

        return optionsMonitor;
    }

    private static TestMemoryHealthCheck CreateMemoryHealthCheck(long allocatedMegabytes, long workingSetMegabytes)
    {
        const long BytesPerMB = 1024L * 1024L;

        var options = CreateOptions(
            warningThreshold: 800, // MB
            criticalThreshold: 1024L, // MB
            workingSetWarningThreshold: 1536L, // MB
            workingSetCriticalThreshold: 2048L); // MB

        var memoryMetrics = new MemoryMetrics(allocatedMegabytes * BytesPerMB, workingSetMegabytes * BytesPerMB, []);

        return new TestMemoryHealthCheck(options, new NullLogger<MemoryHealthCheck>(), memoryMetrics);
    }
}
