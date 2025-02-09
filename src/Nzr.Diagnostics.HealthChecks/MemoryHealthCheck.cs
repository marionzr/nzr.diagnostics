using System.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nzr.Diagnostics.HealthChecks;

/// <summary>
/// Provides memory-based health monitoring for ASP.NET applications and background services.
/// Monitors various memory metrics including GC collections, memory usage, and working set.
/// </summary>
public class MemoryHealthCheck : IHealthCheck, IDisposable
{
    /// <summary>
    /// Key for accessing allocated bytes in the health check result data.
    /// </summary>
    public const string AllocatedBytesDataKey = "AllocatedBytes";

    /// <summary>
    /// Key for accessing working set size in the health check result data.
    /// </summary>
    public const string WorkingSetBytesDataKey = "WorkingSet";

    /// <summary>
    /// Key for accessing private memory size in the health check result data.
    /// </summary>
    public const string PrivateMemorySizeBytesDataKey = "PrivateMemorySize";

    /// <summary>
    /// Key for accessing Gen0 collection count in the health check result data.
    /// </summary>
    public const string Gen0CollectionsDataKey = "Gen0Collections";

    /// <summary>
    /// Key for accessing Gen1 collection count in the health check result data.
    /// </summary>
    public const string Gen1CollectionsDataKey = "Gen1Collections";

    /// <summary>
    /// Key for accessing Gen2 collection count in the health check result data.
    /// </summary>
    public const string Gen2CollectionsDataKey = "Gen2Collections";

    /// <summary>
    /// Key for accessing heap size in the health check result data.
    /// </summary>
    public const string HeapSizeBytesDataKey = "HeapSize";

    /// <summary>
    /// Key for accessing committed memory in the health check result data.
    /// </summary>
    public const string CommittedMemoryDataKey = "CommittedMemory";

    /// <summary>
    /// Key for accessing fragmented memory in the health check result data.
    /// </summary>
    public const string FragmentedMemoryBytesDataKey = "FragmentedMemory";

    /// <summary>
    /// Key for accessing memory load percentage in the health check result data.
    /// </summary>
    public const string MemoryLoadPercentageDataKey = "MemoryLoadPercentage";

    /// <summary>
    /// Key for accessing large object heap size in the health check result data.
    /// </summary>
    public const string LargeObjectHeapSizeBytesDataKey = "LargeObjectHeapSize";

    private readonly MemoryHealthCheckOptions _options;
    private readonly ILogger<MemoryHealthCheck> _logger;
    private Process? _currentProcess;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the MemoryHealthCheck class.
    /// </summary>
    /// <param name="options">Configuration options for memory health monitoring</param>
    /// <param name="logger">Logger for capturing diagnostic information</param>
    /// <exception cref="ArgumentNullException">Thrown when options or logger is null</exception>
    public MemoryHealthCheck(IOptionsMonitor<MemoryHealthCheckOptions> options, ILogger<MemoryHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.CurrentValue;
        var validateOptionsResult = _options.Validate();

        if (!validateOptionsResult.Succeeded)
        {
            throw new InvalidOperationException($"{nameof(MemoryHealthCheckOptions)} is invalid: {validateOptionsResult.FailureMessage}");
        }

        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            _currentProcess ??= Process.GetCurrentProcess() ?? throw new InvalidOperationException("Failed to retrieve current process.");

            if (cancellationToken.IsCancellationRequested)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, "Health check was cancelled");
            }

            var memoryMetrics = await CollectMemoryMetricsAsync(_currentProcess);
            var status = DetermineHealthStatus(_options, memoryMetrics, out var description);
            var result = new HealthCheckResult(status, description, null, memoryMetrics.Data);

            return result;
        }
        catch (OptionsValidationException ex)
        {
            _logger.LogError(ex, "Memory health check options validation failed");
            return new HealthCheckResult(context.Registration.FailureStatus, "Invalid configuration", ex);
        }
        catch (OperationCanceledException)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Health check was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed unexpectedly");
            return new HealthCheckResult(context.Registration.FailureStatus, "Memory health check failed", ex);
        }
    }

    /// <summary>
    /// Collects memory metrics for the current process.
    /// </summary>
    /// <param name="currentProcess">The application process.</param>
    /// <returns>MemoryMetrics with some memory related metrics.</returns>
    protected virtual async Task<MemoryMetrics> CollectMemoryMetricsAsync(Process currentProcess)
    {
        await Task.Yield(); // Ensure we don't block the thread

        var allocatedBytes = GC.GetTotalMemory(forceFullCollection: false);
        var workingSetBytes = currentProcess.WorkingSet64;
        var memoryInfo = GetMemoryInfo();

        var data = new Dictionary<string, object>
        {
            { AllocatedBytesDataKey, allocatedBytes },
            { WorkingSetBytesDataKey, workingSetBytes },
            { PrivateMemorySizeBytesDataKey, currentProcess.PrivateMemorySize64 },
            { Gen0CollectionsDataKey, GC.CollectionCount(0) },
            { Gen1CollectionsDataKey, GC.CollectionCount(1) },
            { Gen2CollectionsDataKey, GC.CollectionCount(2) },
            { HeapSizeBytesDataKey, memoryInfo.HeapSizeBytes },
            { CommittedMemoryDataKey, memoryInfo.TotalCommittedBytes },
            { FragmentedMemoryBytesDataKey, memoryInfo.FragmentedBytes },
            { MemoryLoadPercentageDataKey, GetMemoryLoadPercentage(memoryInfo) },
            { LargeObjectHeapSizeBytesDataKey, memoryInfo.HeapSizeBytes - memoryInfo.FragmentedBytes }
        };

        return new MemoryMetrics(allocatedBytes, workingSetBytes, data);
    }

    /// <summary>
    /// Gets garbage collection memory information.
    /// </summary>
    /// <returns>An object that contains information about the garbage collector's memory usage.</returns>
    private static MemoryInfo GetMemoryInfo()
    {
        var gcMemoryInfo = GC.GetGCMemoryInfo();
        var memoryInfo = MemoryInfo.FromGCMemoryInfo(gcMemoryInfo);

        return memoryInfo;
    }

    private HealthStatus DetermineHealthStatus(MemoryHealthCheckOptions options, MemoryMetrics memoryMetrics, out string statusDescription)
    {
        var criticalThresholdBytes = options.CriticalThreshold.FromMegabytesToBytes();
        var workingSetCriticalThresholdBytes = options.WorkingSetCriticalThreshold.FromMegabytesToBytes();
        var warningThresholdBytes = options.WarningThreshold.FromMegabytesToBytes();
        var workingSetWarningThresholdBytes = options.WorkingSetWarningThreshold.FromMegabytesToBytes();

        // Convert memory values to MB once to avoid repetition
        var allocatedMemoryMB = memoryMetrics.AllocatedBytes.FromBytesToMegabytes();
        var workingSetMemoryMB = memoryMetrics.WorkingSetBytes.FromBytesToMegabytes();

        var metrics = $"Allocated: {allocatedMemoryMB:0}MB, Working Set: {workingSetMemoryMB:0}MB";

        void Log(string message, MemoryHealthCheckOptions options, MemoryMetrics memoryMetrics)
        {
            _logger.LogWarning(
                "{Message}: {Allocated:0}MB (threshold: {WarningThreshold:0}MB), Working Set: {WorkingSet:0}MB (threshold: {WorkingSetWarningThreshold:0}MB)",
                message,
                memoryMetrics.AllocatedBytes.FromBytesToMegabytes(),
                options.WarningThreshold,
                memoryMetrics.WorkingSetBytes.FromBytesToMegabytes(),
                options.WorkingSetWarningThreshold);
        }

        if (memoryMetrics.AllocatedBytes >= criticalThresholdBytes || memoryMetrics.WorkingSetBytes >= workingSetCriticalThresholdBytes)
        {
            Log("Memory usage exceeds critical threshold.", options, memoryMetrics);
            statusDescription = $"Memory usage exceeds critical threshold! {metrics}";

            return HealthStatus.Unhealthy;
        }

        if (memoryMetrics.AllocatedBytes >= warningThresholdBytes || memoryMetrics.WorkingSetBytes >= workingSetWarningThresholdBytes)
        {
            Log("Memory usage is approaching critical levels.", options, memoryMetrics);
            statusDescription = $"Memory usage is approaching critical levels! {metrics}";

            return HealthStatus.Degraded;
        }

        statusDescription = $"Memory usage is within normal range. {metrics}";

        return HealthStatus.Healthy;
    }

    private static double GetMemoryLoadPercentage(MemoryInfo memoryInfo)
    {
        return memoryInfo.HeapSizeBytes / memoryInfo.TotalAvailableMemoryBytes * 100;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes of resources used by the MemoryHealthCheck.
    /// </summary>
    /// <param name="disposing"></param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _currentProcess?.Dispose();
            _currentProcess = null;
        }

        _disposed = true;
    }

    /// <summary>
    /// Represents memory metrics collected during the health check, including information about allocated memory,
    /// working set size, and other memory-related data collected from the process and garbage collector.
    /// </summary>
    /// <param name="AllocatedBytes">
    /// The total allocated memory in bytes, as reported by the .NET runtime's garbage collector.
    /// </param>
    /// <param name="WorkingSetBytes">
    /// The working set size in bytes, which refers to the amount of physical memory (RAM) the process is currently using.
    /// </param>
    /// <param name="Data">
    /// A dictionary containing additional memory-related metrics, such as garbage collection statistics and heap usage information.
    /// </param>
    public sealed record MemoryMetrics(long AllocatedBytes, long WorkingSetBytes, Dictionary<string, object> Data);

    /// <summary>
    /// A record that wraps memory-related information collected during a garbage collection (GC) event.
    /// </summary>
    /// <param name="TotalAvailableMemoryBytes">Total available memory for the GC to use when this GC occurred. If the environment variable DOTNET_GCHeapHardLimit is set, or "Server.GC.HeapHardLimit" is in runtimeconfig.json, this will come from that. If the program is run in a container, this will be an implementation-defined fraction of the container's size. Else, this is the physical memory on the machine that was available for the GC to use when this GC occurred.</param>
    /// <param name="HeapSizeBytes">The total heap size when this GC occurred.</param>
    /// <param name="FragmentedBytes">The total fragmentation when this GC occurred. Fragmentation refers to unused memory between live objects in the heap that can't be allocated to new objects.</param>
    /// <param name="TotalCommittedBytes">The total committed bytes of the managed heap during this GC. This is the memory that the GC has reserved.</param>
    /// <param name="FinalizationPendingCount">The number of objects ready for finalization this GC observed.</param>
    public sealed record MemoryInfo(
        long TotalAvailableMemoryBytes,
        long HeapSizeBytes,
        long FragmentedBytes,
        long TotalCommittedBytes,
        long FinalizationPendingCount
    )
    {
        /// <summary>
        /// Converts a <see cref="GCMemoryInfo"/> to a <see cref="MemoryInfo"/>.
        /// </summary>
        /// <param name="gcMemoryInfo">The <see cref="GCMemoryInfo"/> instance to convert.</param>
        /// <returns>A new <see cref="MemoryInfo"/> record populated with values from the provided <see cref="GCMemoryInfo"/>.</returns>
        public static MemoryInfo FromGCMemoryInfo(GCMemoryInfo gcMemoryInfo)
        {
            return new MemoryInfo(
                gcMemoryInfo.TotalAvailableMemoryBytes,
                gcMemoryInfo.HeapSizeBytes,
                gcMemoryInfo.FragmentedBytes,
                gcMemoryInfo.TotalCommittedBytes,
                gcMemoryInfo.FinalizationPendingCount
            );
        }
    }
}
