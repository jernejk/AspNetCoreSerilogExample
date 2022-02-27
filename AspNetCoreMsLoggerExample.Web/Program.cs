// Logging based on https://github.com/jernejk/AspNetCoreSerilogExample
// NOTE: When upgrading from .NET 5 or earlier, add `<ImplicitUsings>enable</ImplicitUsings>` to **.csproj** file under `<PropertyGroup>`.
// NOTE: You can still use full Program.cs and Startup.cs if you want to and it's similar to .NET 5.

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Register required services.
    builder.Services.AddControllers();
    builder.Services.AddLogging(loggingBuilder =>
    {
        // NOTE: You can add more log providers here.
#if DEBUG
        // Add Seq for local dev.
        loggingBuilder.AddSeq();
#endif
    });

    WebApplication app = builder.Build();

    // Configuration.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }

    // NOTE: If you need UseAuthentication/UseAuthorization, use it after `UseRouting` and before `UseEndpoints`.
    app.UseRouting();
    app.UseEndpoints(endpoints =>
    {
        endpoints.MapControllerRoute(name: "default", pattern: "{controller}/{action=Index}/{id?}");
        endpoints.MapGet("", context => context.Response.WriteAsync("Hello World!\nUse /api/test/flatlog?input=Test, /api/test/StructuredLog?input=Test, etc. and observe console/Seq for logs."));
    });

    app.Run();
}
catch (Exception ex)
{
    // Loading configuration or running the application failed.
    // This will create a logger that can be captured by Azure logger.
    // To enable Azure logger, in Azure Portal:
    // 1. Go to WebApp
    // 2. App Service logs
    // 3. Enable "Application Logging (Filesystem)", "Application Logging (Filesystem)" and "Detailed error messages"
    // 4. Set Retention Period (Days) to 10 or similar value
    // 5. Save settings
    // 6. Under Overview, restart web app
    // 7. Go to Log Stream and observe the logs
    Console.WriteLine("Host terminated unexpectedly: " + ex);
}
