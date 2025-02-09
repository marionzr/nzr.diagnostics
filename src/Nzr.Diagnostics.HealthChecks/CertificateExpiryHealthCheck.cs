using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Nzr.Diagnostics.HealthChecks;

/// <summary>
/// Health check to monitor SSL/TLS certificate expiration.
/// Configuration options allow setting warning and critical thresholds for certificate expiration monitoring.
/// </summary>
public class CertificateExpiryHealthCheck : IHealthCheck, IDisposable
{
    /// <summary>
    /// Key for storing the hostname in the health check result data.
    /// </summary>
    public const string HostnameDataKey = "Hostname";

    /// <summary>
    /// Key for storing the port in the health check result data.
    /// </summary>
    public const string PortDataKey = "Port";

    /// <summary>
    /// Key for storing the expiration date of the certificate in the health check result data (UTC).
    /// </summary>
    public const string ExpiryDateDataKey = "ExpiryDate";

    /// <summary>
    /// Key for storing the number of days remaining until certificate expiration in the health check result data.
    /// </summary>
    public const string DaysRemainingDataKey = "DaysRemaining";

    private readonly CertificateExpiryHealthCheckOptions _options;
    private readonly ILogger<CertificateExpiryHealthCheck> _logger;
    private readonly SemaphoreSlim _semaphore;
    private readonly Dictionary<string, object> _baseData;

    /// <summary>
    /// Initializes a new instance of the <see cref="CertificateExpiryHealthCheck"/> class.
    /// </summary>
    /// <param name="options">Configuration options for certificate expiration monitoring.</param>
    /// <param name="logger">Logger for capturing diagnostic information.</param>
    /// <exception cref="ArgumentNullException">Thrown when options or logger is null.</exception>
    public CertificateExpiryHealthCheck(
        IOptionsMonitor<CertificateExpiryHealthCheckOptions> options,
        ILogger<CertificateExpiryHealthCheck> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _semaphore = new SemaphoreSlim(1, 1);

        _options = options.CurrentValue;

        var validateOptionsResult = _options.Validate();

        if (!validateOptionsResult.Succeeded)
        {
            throw new InvalidOperationException($"{nameof(CertificateExpiryHealthCheckOptions)} is invalid: {validateOptionsResult.FailureMessage}");
        }

        // Pre-allocate the base data dictionary to avoid repeated allocations
        _baseData = new Dictionary<string, object>
        {
            { HostnameDataKey, options.CurrentValue.Hostname },
            { PortDataKey, options.CurrentValue.Port }
        };

        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!await _semaphore.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken))
        {
            _logger.LogWarning("Health check timed out waiting for semaphore");

            return new HealthCheckResult(HealthStatus.Degraded, "Health check is already in progress");
        }

        try
        {
            var data = new Dictionary<string, object>(_baseData);

            try
            {
                var certificate = await GetCertificateAsync(cancellationToken);

                if (certificate == null)
                {
                    _logger.LogError("Failed to retrieve SSL/TLS certificate for {Hostname}", _options.Hostname);
                    return new HealthCheckResult(HealthStatus.Unhealthy, $"Could not retrieve SSL/TLS certificate for {_options.Hostname}.", null, data);
                }

                return ValidateCertificate(certificate, data);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex, "Certificate retrieval timed out for {Hostname}", _options.Hostname);

                return new HealthCheckResult(context.Registration.FailureStatus, "Certificate retrieval timed out", ex, data);
            }
            catch (Exception ex) when (ex is SocketException or IOException)
            {
                _logger.LogError(ex, "Network error occurred while retrieving certificate for {Hostname}", _options.Hostname);

                return new HealthCheckResult(context.Registration.FailureStatus, "Network error occurred while retrieving certificate", ex, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred during certificate health check for {Hostname}", _options.Hostname);

                return new HealthCheckResult(context.Registration.FailureStatus, "Unexpected error during certificate check", ex, data);
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Validates the retrieved certificate and creates appropriate health check result.
    /// </summary>
    private HealthCheckResult ValidateCertificate(X509Certificate2 certificate, Dictionary<string, object> data)
    {
        var expiryDate = certificate.NotAfter.ToUniversalTime();
        var daysRemaining = Math.Round((expiryDate - DateTime.UtcNow).TotalDays);

        data.Add(ExpiryDateDataKey, expiryDate);
        data.Add(DaysRemainingDataKey, daysRemaining);
        certificate.Verify();
        var status = DetermineHealthStatus(daysRemaining, _options);
        var description = $"SSL/TLS certificate for {_options.Hostname} expires in {daysRemaining:N0} days.";

        return new HealthCheckResult(status, description, null, data);
    }

    /// <summary>
    /// Retrieves the SSL/TLS certificate for a given hostname and port.
    /// </summary>
    protected virtual async Task<X509Certificate2?> GetCertificateAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.TimeoutMs);

        using var tcpClient = new TcpClient();
        await tcpClient.ConnectAsync(_options.Hostname, _options.Port, cts.Token);

        using var stream = tcpClient.GetStream();
        using var sslStream = new SslStream(stream, false, new RemoteCertificateValidationCallback((_, cert, _, _) => cert != null));

        await sslStream.AuthenticateAsClientAsync(_options.Hostname);
        var certificate = sslStream.RemoteCertificate as X509Certificate2;

        return certificate;
    }

    /// <summary>
    /// Determines the health status based on the remaining days before the certificate expires.
    /// </summary>
    private HealthStatus DetermineHealthStatus(double daysRemaining, CertificateExpiryHealthCheckOptions options)
    {
        if (daysRemaining <= options.CriticalThresholdDays)
        {
            _logger.LogError(
                "SSL/TLS certificate for {Hostname} expires in {DaysRemaining:N0} days (Critical threshold: {CriticalThreshold})",
                options.Hostname, daysRemaining, options.CriticalThresholdDays);

            return HealthStatus.Unhealthy;
        }

        if (daysRemaining <= options.WarningThresholdDays)
        {
            _logger.LogWarning(
                "SSL/TLS certificate for {Hostname} expires in {DaysRemaining:N0} days (Warning threshold: {WarningThreshold})",
                options.Hostname, daysRemaining, options.WarningThresholdDays);

            return HealthStatus.Degraded;
        }

        return HealthStatus.Healthy;
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
            _semaphore.Dispose();
        }
    }
}
