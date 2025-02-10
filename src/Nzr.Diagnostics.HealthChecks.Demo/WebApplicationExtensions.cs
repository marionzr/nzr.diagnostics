using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Nzr.Diagnostics.HealthChecks.Demo;

namespace Nzr.Diagnostics.HealthChecks.Demo;

/// <summary>
/// Extension methods for <see cref="WebApplication"/> to map health check routes and UI.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Maps health check routes for monitoring the health status of the application.
    /// Provides the main health check endpoint and UI for visual monitoring.
    /// </summary>
    /// <param name="app">The WebApplication instance.</param>
    /// <returns>The updated WebApplication with health check routes mapped.</returns>
    public static WebApplication MapHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/healthz", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        })
        .ShortCircuit();

        app.MapHealthChecksUI(options =>
        {
            CopyEmbeddedCssToWebRootDirectory(app);
            options.UIPath = "/healthz-ui";

            // You can customize the logo nzr.png inside the wwwroot.healthz folder
            options.AddCustomStylesheet($"wwwroot{Path.DirectorySeparatorChar}healthz{Path.DirectorySeparatorChar}nzr.css");
        });

        return app;
    }

    private static void CopyEmbeddedCssToWebRootDirectory(WebApplication app)
    {
        const string resourceName = "Nzr.Diagnostics.HealthChecks.nzr.css";
        var webRootPath = app.Environment.WebRootPath;
        var cssFilePath = Path.Combine(webRootPath, $"healthz{Path.DirectorySeparatorChar}nzr.css");

        if (!File.Exists(cssFilePath))
        {
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .Single(a => a.GetName().Name == "Nzr.Diagnostics.HealthChecks");

            using var stream = assembly.GetManifestResourceStream(resourceName)!;
            using var fileStream = new FileStream(cssFilePath, FileMode.Create, FileAccess.Write);
            stream.CopyTo(fileStream);
        }
    }
}
