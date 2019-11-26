using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Diagnostics;

namespace AspNetCoreSerilogExample.Web
{
    /// <summary>
    /// Logging based on blog post https://jkdev.me/asp-net-core-serilog/
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            // There are 2 reasons why we are building logging this early and is used only for logging.
            // 1. We want to log any issues that might happen while the server is spinning up.
            //    Those are hard to find bugs and quite often are not logged properly.
            // 2. Unfortunately ".UseConfiguration()" doesn't work correctly in the .NET Core 2.2.5 (the version I made this demo).
            //    It will ignore the configuration and will load default configuration.
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                .AddJsonFile("appsettings.local.json", optional: true)
                .Build();

            // Lets make sure that if creating web host fails, we can log that error.
            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ApplicationName", typeof(Program).Assembly.GetName().Name);

#if DEBUG
            // Used to filter out potentially bad data due debugging.
            // Very useful when doing Seq dashboards and want to remove logs under debugging session.
            loggerConfiguration.Enrich.WithProperty("DebuggerAttached", Debugger.IsAttached);
#endif

            // When using ".UseSerilog()" it will use "Log.Logger".
            Log.Logger = loggerConfiguration.CreateLogger();

            try
            {
                // In some rare cases, creating web host can fail.
                // This style of logging increases the chances of logging this issue.
                Log.Logger.Information("Bootstrapping web app...");
                using (var host = CreateWebHostBuilder(args).Build())
                {
                    host.Run();
                }
            }
            catch (Exception e)
            {
                // Happens rarely but when it does, you'll thank me. :)
                Log.Logger.Fatal(e, "Unable to bootstrap web app.");
            }

            // Make sure all the log sinks have processed the last log before closing the application.
            Log.CloseAndFlush();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                            .UseStartup<Startup>()
                            .ConfigureAppConfiguration(configuration =>
                            {
                                // It's a good practice to add local settings for local dev.
                                configuration.AddJsonFile("appsettings.local.json", optional: true);
                            })
                            .UseSerilog();
    }
}
