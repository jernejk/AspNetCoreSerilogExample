using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Diagnostics;

namespace AspNetCoreSerilogExample.Web
{
    public class Program
    {
        public static IConfiguration Configuration { get; } = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
            // Local settings.
            .AddJsonFile("appsettings.local.json", optional: true)
            .Build();

        public static int Main(string[] args)
        {
            // Lets make sure that if creating web host fails, we can log that error.
            var loggerConfiguration = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
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

            return 0;
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
            => WebHost.CreateDefaultBuilder(args)
                           .UseStartup<Startup>()
                           .UseConfiguration(Configuration)
                           .UseSerilog();
    }
}
