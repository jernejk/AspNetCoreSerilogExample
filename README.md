# ASP.NET Core 6.0 Serilog Template

This is an example of how to create a ASP .NET Core app with Serilog (.NET 6+)

Check my blog post for more details: [ASP.NET Core + Serilog](https://jkdev.me/asp-net-core-serilog/)

If you're looking for .NET (Core) 2.2, 3.1 or 5, checkout the old branches: https://github.com/jernejk/AspNetCoreSerilogExample/branches

## 1. Add Nuget packages

In `csproj` add:

``` xml
  <ItemGroup>
    <!-- Serilog dependencies -->
    <PackageReference Include="Serilog" Version="2.9.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="3.2.0" />
    <PackageReference Include="Serilog.Enrichers.Environment" Version="2.1.3" />
    <PackageReference Include="Serilog.Exceptions" Version="5.3.1" />
    <PackageReference Include="Serilog.Extensions.Logging" Version="3.0.1" />
    <PackageReference Include="Serilog.Settings.Configuration" Version="3.1.0" />
    <PackageReference Include="Serilog.Sinks.Async" Version="1.4.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="3.1.1" />
    <PackageReference Include="Serilog.Sinks.RollingFile" Version="3.3.0" />
    <PackageReference Include="Serilog.Sinks.Seq" Version="4.0.0" />
  </ItemGroup>

  <ItemGroup>
    <!-- Make sure all of the necessary appsettings are included with the application. -->
    <Content Update="appsettings*.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <CopyToPublishDirectory>Always</CopyToPublishDirectory>
    </Content>
    <Content Update="appsettings.Local.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
      <CopyToPublishDirectory>Never</CopyToPublishDirectory>
    </Content>
  </ItemGroup>
```

## 2. Add Serilog configuration

Add Seq and async console configuration in `appsetings.json`:

``` js
  "Serilog": {
    "Using": [ "Serilog.Exceptions", "Serilog", "Serilog.Sinks.Console", "Serilog.Sinks.Seq" ],
    "MinimumLevel": {
      "Default": "Verbose",
      "Override": {
        "System": "Information",
        "Microsoft": "Information",
        "Microsoft.EntityFrameworkCore": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://localhost:5341",
          "apiKey": "none",
          "restrictedToMinimumLevel": "Verbose"
        }
      },
      {
        "Name": "Async",
        "Args": {
          "configure": [
            {
              "Name": "Console",
              "Args": {
                "restrictedToMinimumLevel": "Information"
              }
            }
          ]
        }
      }
    ],
    "Enrich": [ "FromLogContext", "WithExceptionDetails" ],
    "Properties": {
      "Environment": "LocalDev"
    }
  }
```

## 3. Update Program.cs (minimal APIs)

``` cs
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
```
