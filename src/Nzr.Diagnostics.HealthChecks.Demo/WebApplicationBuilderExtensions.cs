using Microsoft.Extensions.Diagnostics.HealthChecks;
using Nzr.Diagnostics.HealthChecks.Demo;

namespace Nzr.Diagnostics.HealthChecks.Demo;

/// <summary>
/// Extension methods for <see cref="WebApplicationBuilder"/> to set up and configure health checks
/// and related services like configuration sources and domain dependencies.
/// </summary>
public static class WebApplicationBuilderExtensions
{
    /// <summary>
    /// Adds health checks to the application.
    /// Configures memory and certificate expiry health checks and integrates them with the health check system.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance.</param>
    /// <returns>The updated WebApplicationBuilder with health checks added.</returns>
    public static WebApplicationBuilder AddHealthChecks(this WebApplicationBuilder builder)
    {
        // Configure health check options from configuration files.
        builder.Services.Configure<MemoryHealthCheckOptions>(builder.Configuration.GetSection("MemoryHealthCheck"));
        builder.Services.Configure<CertificateExpiryHealthCheckOptions>(builder.Configuration.GetSection("CertificateExpiryHealthCheck"));

        // Add the health checks to the service collection.
        builder.Services
            .AddHealthChecks()
            .AddCheck<MemoryHealthCheck>("Memory Health Check", failureStatus: HealthStatus.Unhealthy, tags: ["system"], timeout: TimeSpan.FromSeconds(1))
            .AddCheck<CertificateExpiryHealthCheck>("Certificate Expiry Health Check", failureStatus: HealthStatus.Unhealthy, tags: ["security"], timeout: TimeSpan.FromSeconds(5))
            .AddCheck<ThreadPoolHealthCheck>("ThreadPool Health Check", failureStatus: HealthStatus.Unhealthy, tags: ["system"], timeout: TimeSpan.FromSeconds(1));

        // Configure health checks UI to be used for monitoring system health in a web UI.
        builder.Services
            .AddHealthChecksUI(setup =>
            {
                setup.SetHeaderText("Nzr Health Checks Status");
                setup.AddHealthCheckEndpoint("Nzr", "/healthz");
                setup.SetApiMaxActiveRequests(1);
                setup.MaximumHistoryEntriesPerEndpoint(60);
                setup.SetEvaluationTimeInSeconds(5);

            })
            .AddInMemoryStorage();


        if (builder.Environment.EnvironmentName == "Unhealthy")
        {
            // For testing purposes only: Lower the min and max worker threads to cause starvation
            ThreadPool.SetMinThreads(1, 1);
            ThreadPool.SetMaxThreads(2, 2);
        }

        return builder;
    }

    /// <summary>
    /// Adds configuration sources to the application from multiple JSON files and environment variables.
    /// </summary>
    /// <param name="builder">The WebApplicationBuilder instance.</param>
    /// <returns>The updated WebApplicationBuilder with configuration sources added.</returns>
    public static WebApplicationBuilder AddConfigurationSources(this WebApplicationBuilder builder)
    {
        builder.Configuration
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

        return builder;
    }
}
