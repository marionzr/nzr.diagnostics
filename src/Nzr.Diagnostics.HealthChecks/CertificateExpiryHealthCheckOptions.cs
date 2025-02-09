using Microsoft.Extensions.Options;

namespace Nzr.Diagnostics.HealthChecks;

/// <summary>
/// Options for configuring SSL/TLS certificate health checks.
/// </summary>
public class CertificateExpiryHealthCheckOptions
{
    private const int DefaultPort = 443;

    /// <summary>
    /// The hostname of the service to check.
    /// </summary>
    public required string Hostname { get; set; }

    /// <summary>
    /// The port to use for checking the SSL/TLS certificate.
    /// If not explicitly set, it attempts to extract the port from the ASPNETCORE_URLS environment variable.
    /// Defaults to 443 if no valid port is found.
    /// </summary>
    public int Port { get; set; } = GetPortFromEnvironment() ?? DefaultPort;

    /// <summary>
    /// The number of days before expiration to trigger a warning status.
    /// </summary>
    public required int WarningThresholdDays { get; set; } = 30;

    /// <summary>
    /// The number of days before expiration to trigger a critical (unhealthy) status.
    /// </summary>
    public int CriticalThresholdDays { get; set; } = 10;

    /// <summary>
    /// Timeout in milliseconds for the certificate retrieval operation.
    /// </summary>
    public int TimeoutMs { get; set; } = 5000;

    /// <summary>
    /// Extracts the port number from the ASPNETCORE_URLS environment variable.
    /// </summary>
    /// <returns>The extracted port number, or null if not found.</returns>
    private static int? GetPortFromEnvironment()
    {
        var urls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");

        if (string.IsNullOrWhiteSpace(urls))
        {
            return null;
        }

        var urlParts = urls.Split(';')
            .Select(url => new Uri(url))
            .Where(uri => uri.Scheme == Uri.UriSchemeHttps) // Prefer HTTPS
            .Select(uri => uri.Port)
            .FirstOrDefault();

        return urlParts > 0 ? urlParts : null;
    }

    /// <summary>
    /// Validates the options.
    /// </summary>
    /// <returns>A ValidateOptionsResult result.</returns>
    public ValidateOptionsResult Validate()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(Hostname))
            {
                return ValidateOptionsResult.Fail("Hostname must be specified");
            }
            else if (Port is <= 0 or > 65535)
            {
                return ValidateOptionsResult.Fail($"Port must be between 1 and 65535, got {Port}");
            }
            else if (WarningThresholdDays <= CriticalThresholdDays)
            {
                return ValidateOptionsResult.Fail($"Warning threshold ({WarningThresholdDays}) must be greater than critical threshold ({CriticalThresholdDays})");
            }
            else if (TimeoutMs <= 0)
            {
                return ValidateOptionsResult.Fail($"Timeout must be greater than 0, got {TimeoutMs}");
            }

            return ValidateOptionsResult.Success;
        }
        catch (Exception ex)
        {
            return ValidateOptionsResult.Fail($"Validation failed: {ex.Message}");
        }
    }
}
