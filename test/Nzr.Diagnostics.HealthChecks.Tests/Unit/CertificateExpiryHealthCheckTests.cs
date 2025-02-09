using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using static Nzr.Diagnostics.HealthChecks.CertificateExpiryHealthCheck;

namespace Nzr.Diagnostics.HealthChecks.Tests.Unit;

public class CertificateExpiryHealthCheckTests
{
    private const string Hostname = "nzr.com";
    private const int Port = 443;

    [Fact]
    public async Task CheckHealthAsync_When_Certificate_Not_Found_Should_Return_Unhealthy()
    {
        // Arrange

        var healthCheck = CreateCertificateExpiryHealthCheck(DateTime.UtcNow /*any*/, false);

        // Act

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());


        // Assert

        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Data[HostnameDataKey].Should().Be(Hostname);
        result.Data[PortDataKey].Should().Be(Port);
        result.Description.Should().Be($"Could not retrieve SSL/TLS certificate for {Hostname}.");
    }

    [Fact]
    public async Task CheckHealthAsync_When_Certificate_ExpiryDate_Below_Critical_Threashold_Should_Return_Unhealthy()
    {
        // Arrange

        var expiryDate = DateTime.UtcNow.AddDays(9); // Below critical threshold
        var healthCheck = CreateCertificateExpiryHealthCheck(expiryDate);

        // Act

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());


        // Assert

        result.Status.Should().Be(HealthStatus.Unhealthy);
        AssertData(result, expiryDate, 9, out var dataDaysRemaining);
        result.Description.Should().Be($"SSL/TLS certificate for {Hostname} expires in {dataDaysRemaining:N0} days.");
    }

    [Fact]
    public async Task CheckHealthAsync_When_Certificate_ExpiryDate_Below_Warning_Threashold_Should_Return_Degraded()
    {
        // Arrange

        var expiryDate = DateTime.UtcNow.AddDays(14); // Below warning threshold
        var healthCheck = CreateCertificateExpiryHealthCheck(expiryDate);

        // Act

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());


        // Assert

        result.Status.Should().Be(HealthStatus.Degraded);
        AssertData(result, expiryDate, 14, out var dataDaysRemaining);
        result.Description.Should().Be($"SSL/TLS certificate for {Hostname} expires in {dataDaysRemaining:N0} days.");
    }

    [Fact]
    public async Task CheckHealthAsync_When_Certificate_ExpiryDate_Above_Threasholds_Should_Return_Healthy()
    {
        // Arrange

        var expiryDate = DateTime.UtcNow.AddDays(20); // Above thresholds
        var healthCheck = CreateCertificateExpiryHealthCheck(expiryDate);

        // Act

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());


        // Assert

        result.Status.Should().Be(HealthStatus.Healthy);
        AssertData(result, expiryDate, 20, out var dataDaysRemaining);
        result.Description.Should().Be($"SSL/TLS certificate for {Hostname} expires in {dataDaysRemaining:N0} days.");
    }

    private static void AssertData(HealthCheckResult result, DateTime expiryDate, int dataDaysRemainingExpected, out double dataDaysRemaining)
    {
        var dataHostname = (string)result.Data[HostnameDataKey];
        var dataPort = (int)result.Data[PortDataKey];
        var dataExpiryDate = (DateTime)result.Data[ExpiryDateDataKey];
        dataDaysRemaining = (double)result.Data[DaysRemainingDataKey];
        dataHostname.Should().Be(Hostname);
        dataPort.Should().Be(dataPort);
        dataExpiryDate.Should().BeCloseTo(expiryDate, TimeSpan.FromSeconds(1));
        dataDaysRemaining.Should().BeLessThanOrEqualTo(dataDaysRemainingExpected);
    }

    private class TestCertificateExpiryHealthCheck : CertificateExpiryHealthCheck
    {
        private readonly X509Certificate2? _certificate;

        public TestCertificateExpiryHealthCheck(
            IOptionsMonitor<CertificateExpiryHealthCheckOptions> options,
            ILogger<CertificateExpiryHealthCheck> logger,
            X509Certificate2? mockCertificate)
            : base(options, logger)
        {
            _certificate = mockCertificate;
        }

        protected override Task<X509Certificate2?> GetCertificateAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_certificate);
        }
    }

    private static IOptionsMonitor<CertificateExpiryHealthCheckOptions> CreateOptions(int warningThreshold, int criticalThreshold)
    {
        var options = new CertificateExpiryHealthCheckOptions
        {
            Hostname = Hostname,
            Port = Port,
            WarningThresholdDays = warningThreshold,
            CriticalThresholdDays = criticalThreshold
        };

        var optionsMonitor = Substitute.For<IOptionsMonitor<CertificateExpiryHealthCheckOptions>>();
        optionsMonitor.CurrentValue.Returns(options);

        return optionsMonitor;
    }

    private static TestCertificateExpiryHealthCheck CreateCertificateExpiryHealthCheck(DateTime expiryDate, bool certificateFound = true)
    {
        var options = CreateOptions(15, 10);
        var certificate = certificateFound ? CreateCertificateWithExpiry(expiryDate) : null;
        var certificateExpiryHealthCheck = new TestCertificateExpiryHealthCheck(options, new NullLogger<TestCertificateExpiryHealthCheck>(), certificate);

        return certificateExpiryHealthCheck;
    }


    /// <summary>
    /// Helper method to simulate certificate loading with custom expiry
    /// </summary>
    /// <param name="expiryDate"></param>
    /// <returns></returns>
    private static X509Certificate2 CreateCertificateWithExpiry(DateTime expiryDate)
    {
        using var rsa = RSA.Create(2048);
        var certificateRequest = new CertificateRequest(
            "CN=Test Certificate",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // Create the self-signed certificate with an expiry date
        var cert = certificateRequest.CreateSelfSigned(DateTimeOffset.UtcNow, expiryDate);

        return new X509Certificate2(cert);
    }
}
