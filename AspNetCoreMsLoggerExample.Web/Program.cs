using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace AspNetCoreMsLoggerExample.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                using IHost host = CreateHostBuilder(args).Build();
                host.Run();
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
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                   .ConfigureWebHostDefaults(webBuilder =>
                   {
                       webBuilder.UseStartup<Startup>()
                        .CaptureStartupErrors(true)
                        .ConfigureAppConfiguration(config =>
                        {
                            config
                                // Used for local settings like connection strings.
                                .AddJsonFile("appsettings.Local.json", optional: true);
                        })
                        .ConfigureLogging((hostingContext, loggingBuilder) => {
#if DEBUG
                            // Add Seq for local dev.
                            loggingBuilder.AddSeq();
#endif
                        });
                   });
    }
}
