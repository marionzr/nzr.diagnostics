using Nzr.Diagnostics.HealthChecks.Demo;

var builder = WebApplication.CreateBuilder(args);
builder.AddConfigurationSources();
builder.AddHealthChecks();

var app = builder.Build();
app.UseStaticFiles();
app.MapHealthChecks();

if (builder.Environment.EnvironmentName == "Unhealthy")
{
    // Create some pressure on the thread pool
    var tasks = new List<Task>();

    for (var i = 0; i < 100; i++)
    {
        tasks.Add(Task.Run(async () =>
        {
            await Task.Delay(5000); // Simulate work
        }));
    }
}


await app.RunAsync();
