using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Snapshooter.Xunit;
namespace Nzr.Diagnostics.HealthChecks.Tests.Integration;

public class MemoryHealthCheckTests
{
    [Fact]
    public async Task MemoryHealthCheck_ShouldReturnHealthy_WhenMemoryIsNormal()
    {
        // Arrange

        // Setting the thresholds to the maximum to avoid flaky tests
        var optionsMonitor = CreateOptions(int.MaxValue - 1, int.MaxValue, int.MaxValue - 1, int.MaxValue);
        var healthCheck = new MemoryHealthCheck(optionsMonitor, new NullLogger<MemoryHealthCheck>());

        // Act

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext(), CancellationToken.None);

        // Assert

        result.Should().MatchSnapshot(options => options
            .IgnoreFields(nameof(HealthCheckResult.Data))
            .IgnoreField(nameof(HealthCheckResult.Description)));
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
}
