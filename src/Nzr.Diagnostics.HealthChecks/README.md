# Nzr.Diagnostics.HealthChecks

![NuGet Version](https://img.shields.io/nuget/v/Nzr.Diagnostics.HealthChecks)
![NuGet Downloads](https://img.shields.io/nuget/dt/Nzr.Diagnostics.HealthChecks)
![GitHub last commit](https://img.shields.io/github/last-commit/marionzr/nzr.diagnostics)
![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/marionzr/nzr.diagnostics/build-test-and-publish.yml)
![GitHub License](https://img.shields.io/github/license/marionzr/nzr.diagnostics)

`HealthChecks` is a .NET library designed for integrating memory and certificate expiry health checks into your application, allowing you to monitor the system's health and ensure it meets required thresholds. It integrates with the Microsoft HealthChecks framework and supports both operational and certificate health check monitoring. The library provides configuration and services for health check monitoring through simple integrations.
[Official documentation](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-8.0)

![Nzr.Diagnostics.HealthChecks.Demo](https://raw.githubusercontent.com/marionzr/nzr.diagnostics/main/assets/demo.gif)

## Getting Started

### Installation

To install the Nzr.Diagnostics.HealthChecks library, use the NuGet Package Manager:

```package-manager
Install-Package Nzr.Diagnostics.HealthChecks
```

or

```bash
dotnet add package Nzr.Diagnostics.HealthChecks
```

### Usage

1 - Configure health check options from configuration files

```csharp
WebApplicationBuilder builder ...

// Configure health check options from configuration files.
builder.Services.Configure<MemoryHealthCheckOptions>(builder.Configuration.GetSection("MemoryHealthCheck"));
builder.Services.Configure<CertificateExpiryHealthCheckOptions>(builder.Configuration.GetSection("CertificateExpiryHealthCheck"));
```

2 - Add the health checks to the service collection:

```csharp
builder.Services
    .AddHealthChecks()
    .AddCheck<MemoryHealthCheck>("Memory HealthCheck", failureStatus: HealthStatus.Degraded, tags: ["system"]);
```

3 - Configure health checks UI to be used for monitoring system health in a web UI:

```csharp
builder.Services
    .AddHealthChecksUI(setup =>
    {
        setup.AddHealthCheckEndpoint("Nzr", "/healthz");
    })
    .AddInMemoryStorage();
```

4 - Map health checks and the UI:

```csharp
app.MapHealthChecks("/healthz", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
})
.ShortCircuit();

app.MapHealthChecksUI(options =>
{
    options.UIPath = "/healthz-ui";
});
```

---

## Health Checks

### Memory Health Check

The MemoryHealthCheck class works by continuously monitoring key memory metrics of your application in real time.
It collects data such as total allocated memory, the working set size, garbage collection counts, and detailed
GC memory information (including heap size, committed memory, and fragmentation) using built-in .NET APIs.
This data is then converted into human-readable formats and compared against configurable thresholds to determine
whether the system is operating under healthy, degraded, or unhealthy conditions.
The benefits of using this health check include early detection of memory pressure issues, enhanced diagnostics
through comprehensive memory metrics, and the ability to proactively manage and optimize system performance—all
while ensuring that the health check itself operates asynchronously and safely within your application's lifecycle.

## License

Nzr.Diagnostics.HealthChecks is licensed under the Apache License, Version 2.0, January 2004. You may obtain a copy of the License at:

http://www.apache.org/licenses/LICENSE-2.0

## Disclaimer

This project is provided "as-is" without any warranty or guarantee of its functionality. The author assumes no responsibility or liability for any issues, damages, or consequences arising from the use of this code, whether direct or indirect. By using this project, you agree that you are solely responsible for any risks associated with its use, and you will not hold the author accountable for any loss, injury, or legal ramifications that may occur.

Please ensure that you understand the code and test it thoroughly before using it in any production environment.
