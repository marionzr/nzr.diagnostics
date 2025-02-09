using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Nzr.Diagnostics.HealthChecks;

/// <summary>
/// A health check for monitoring the status of the thread pool in an application.
/// It checks the thread pool's configuration, available threads, active threads,
/// and detects potential starvation conditions for both worker and completion port threads.
/// </summary>
public class ThreadPoolHealthCheck : IHealthCheck
{
    private readonly ILogger<ThreadPoolHealthCheck> _logger;

    /// <summary>
    /// The for accessing the minimum worker threads in the health check result data.
    /// </summary>
    public const string MinWorkerThreadsDataKey = "MinWorkerThreads";

    /// <summary>
    /// The for accessing the minimum Completion Port threads in the health check result data.
    /// </summary>
    public const string MinCompletionPortThreadsDataKey = "MinCompletionPortThreads";

    /// <summary>
    /// The for accessing the maximum worker threads in the health check result data.
    /// </summary>
    public const string MaxWorkerThreadsDataKey = "MaxWorkerThreadsData";

    /// <summary>
    /// The for accessing the maximum Completion Port threads in the health check result data.
    /// </summary>
    public const string MaxCompletionPortThreadsDataKey = "MaxCompletionPortThreads";

    /// <summary>
    /// The for accessing the available worker threads in the health check result data.
    /// </summary>
    public const string AvailableWorkerThreadsDataKey = "AvailableWorkerThreads";

    /// <summary>
    /// The for accessing the available Completion Port threads in the health check result data.
    /// </summary>
    public const string AvailableCompletionPortThreadsDataKey = "AvailableCompletionPortThreads";

    /// <summary>
    /// The for accessing the active worker threads in the health check result data.
    /// </summary>
    public const string ActiveWorkerThreadsDataKey = "ActiveWorkerThreads";

    /// <summary>
    /// The for accessing the active Completion Port threads in the health check result data.
    /// </summary>
    public const string ActiveCompletionPortThreadsDataKey = "ActiveCompletionPortThreads";

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadPoolHealthCheck"/> class.
    /// </summary>
    /// <param name="logger">The logger used to log health check results and potential errors.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public ThreadPoolHealthCheck(ILogger<ThreadPoolHealthCheck> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Performs the health check for the thread pool.
    /// It checks the current thread pool status, including the number of available,
    /// active, and minimum worker and Completion Port threads. It will log any starvation conditions.
    /// </summary>
    /// <param name="context">The health check context, which provides metadata for the health check.</param>
    /// <param name="cancellationToken">A token to observe while waiting for the check to complete.</param>
    /// <returns>A task representing the asynchronous operation, with a <see cref="HealthCheckResult"/> indicating the status of the health check.</returns>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get thread pool configuration and current status
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableCompletionPortThreads);
            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);

            // Calculate active thread counts
            var activeWorkerThreads = maxWorkerThreads - availableWorkerThreads;
            var activeCompletionPortThreads = maxCompletionPortThreads - availableCompletionPortThreads;

            // Prepare the data for the health check result
            var data = new Dictionary<string, object>
            {
                { AvailableWorkerThreadsDataKey, availableWorkerThreads },
                { AvailableCompletionPortThreadsDataKey, availableCompletionPortThreads },
                { MinWorkerThreadsDataKey, minWorkerThreads },
                { MinCompletionPortThreadsDataKey, minCompletionPortThreads },
                { MaxWorkerThreadsDataKey, maxWorkerThreads },
                { MaxCompletionPortThreadsDataKey, maxCompletionPortThreads },
                { ActiveWorkerThreadsDataKey, activeWorkerThreads },
                { ActiveCompletionPortThreadsDataKey, activeCompletionPortThreads }
            };

            // Create a descriptive message with the collected values
            var metrics = $"Min Worker Threads: {minWorkerThreads}, Min Completion Port Threads: {minCompletionPortThreads}, " +
                          $"Max Worker Threads: {maxWorkerThreads}, Max Completion Port Threads: {maxCompletionPortThreads}, " +
                          $"Available Worker Threads: {availableWorkerThreads}, Available Completion Port Threads: {availableCompletionPortThreads}, " +
                          $"Active Worker Threads: {activeWorkerThreads}, Active Completion Port Threads: {activeCompletionPortThreads}";

            // Check for Worker Thread starvation condition
            var isWorkerThreadStarvation = activeWorkerThreads > minWorkerThreads;
            // Check for Completion Port Thread starvation condition
            var isCompletionPortThreadStarvation = activeCompletionPortThreads > minCompletionPortThreads;

            // If either condition is true, we have starvation.
            if (isWorkerThreadStarvation || isCompletionPortThreadStarvation)
            {
                // Log the starvation conditions
                if (isWorkerThreadStarvation)
                {
                    var diffWorker = activeWorkerThreads - minWorkerThreads;
                    _logger.LogCritical(
                        "Thread Pool Worker Thread Starvation Detected - Active Worker Threads: {ActiveWorkerThreads}, Min Worker Threads: {MinWorkerThreads}, Excess: {ExcessWorkerThreads}",
                        activeWorkerThreads, minWorkerThreads, diffWorker);
                }

                if (isCompletionPortThreadStarvation)
                {
                    var diffIo = activeCompletionPortThreads - minCompletionPortThreads;
                    _logger.LogCritical(
                        "Thread Pool Completion Port Thread Starvation Detected - Active Completion Port Threads: {ActiveIoThreads}, Min Completion Port Threads: {MinIoThreads}, Excess: {ExcessIoThreads}",
                        activeCompletionPortThreads, minCompletionPortThreads, diffIo);
                }

                var description = $"Thread Pool Starvation Detected: {metrics}";

                return Task.FromResult(HealthCheckResult.Unhealthy(description, data: data));
            }
            else
            {
                var description = $"Thread Pool is healthy: {metrics}";
                return Task.FromResult(HealthCheckResult.Healthy(description, data));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while checking the thread pool status.");
            return Task.FromResult(new HealthCheckResult(context.Registration.FailureStatus, "Thread pool health check failed.", ex));
        }
    }
}
