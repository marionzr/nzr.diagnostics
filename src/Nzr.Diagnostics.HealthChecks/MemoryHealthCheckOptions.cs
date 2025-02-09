using Microsoft.Extensions.Options;

namespace Nzr.Diagnostics.HealthChecks;

/// <summary>
/// Configuration options for memory health monitoring.
/// Can be configured via appsettings.json under "MemoryHealthCheck" section.
/// </summary>
public class MemoryHealthCheckOptions
{
    /// <summary>
    /// Warning threshold for allocated memory in megabytes
    /// Default: 800MB
    /// </summary>
    public long WarningThreshold { get; set; } = 800L; // In MB

    /// <summary>
    /// Critical threshold for allocated memory in megabytes
    /// Default: 1GB
    /// </summary>
    public long CriticalThreshold { get; set; } = 1024L; // In MB

    /// <summary>
    /// Warning threshold for working set in megabytes
    /// Default: 1.5GB
    /// </summary>
    public long WorkingSetWarningThreshold { get; set; } = 1536L; // In MB

    /// <summary>
    /// Critical threshold for working set in megabytes
    /// Default: 2GB
    /// </summary>
    public long WorkingSetCriticalThreshold { get; set; } = 2048L; // In MB

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <returns>A ValidateOptionsResult result.</returns>
    public ValidateOptionsResult Validate()
    {
        if (WarningThreshold >= CriticalThreshold)
        {
            return ValidateOptionsResult.Fail("Warning threshold must be less than critical threshold");
        }
        else if (WorkingSetWarningThreshold >= WorkingSetCriticalThreshold)
        {
            return ValidateOptionsResult.Fail("Working set warning threshold must be less than critical threshold");
        }

        return ValidateOptionsResult.Success;
    }
}
