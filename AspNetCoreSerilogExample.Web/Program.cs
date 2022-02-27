// Logging based on https://github.com/jernejk/AspNetCoreSerilogExample and https://github.com/datalust/dotnet6-serilog-example
// NOTE: When upgrading from .NET 5 or earlier, add `<ImplicitUsings>enable</ImplicitUsings>` to **.csproj** file under `<PropertyGroup>`.
// NOTE: While you can still use full Program.cs and Startup.cs, `.UseSerilog()` is marked as obsolete for them. It's safer to move to minimal APIs.
using Serilog;
using System.Diagnostics;

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(ctx.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", typeof(Program).Assembly.GetName().Name)
            .Enrich.WithProperty("Environment", ctx.HostingEnvironment);

#if DEBUG
        // Used to filter out potentially bad data due debugging.
        // Very useful when doing Seq dashboards and want to remove logs under debugging session.
        loggerConfiguration.Enrich.WithProperty("DebuggerAttached", Debugger.IsAttached);
#endif
    });

    // Register services
    builder.Services.AddControllers();
    builder.Services.AddLogging();


    WebApplication app = builder.Build();

    // Rest of configuration.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // This will make the HTTP requests log as rich logs instead of plain text.
    app.UseSerilogRequestLogging();

    app.UseRouting();

    // Absolute minimum setup, just return "Hello world!" to browser.
    // You can use Controllers, SPA routing, SignalR, etc. routing.
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllerRoute(name: "default", pattern: "{controller}/{action=Index}/{id?}");
        endpoints.MapGet("", context => context.Response.WriteAsync("Hello World!\nUse /api/test/flatlog?input=Test, /api/test/StructuredLog?input=Test, etc. and observe console/Seq for logs."));
    });

    app.Run();
}
catch (Exception ex)
{
    if (Log.Logger == null || Log.Logger.GetType().Name == "SilentLogger")
    {
        // Loading configuration or Serilog failed.
        // This will create a logger that can be captured by Azure logger.
        // To enable Azure logger, in Azure Portal:
        // 1. Go to WebApp
        // 2. App Service logs
        // 3. Enable "Application Logging (Filesystem)", "Application Logging (Filesystem)" and "Detailed error messages"
        // 4. Set Retention Period (Days) to 10 or similar value
        // 5. Save settings
        // 6. Under Overview, restart web app
        // 7. Go to Log Stream and observe the logs
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
    }

    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.Information("Shut down complete");
    Log.CloseAndFlush();
}
