using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prevueit.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        //public static IHostBuilder CreateHostBuilder(string[] args)
        //{
        //    return Host.CreateDefaultBuilder(args)
        //        .ConfigureAppConfiguration((hostingContext, config) =>
        //        {
        //            config.Sources.Clear();
        //            var env = hostingContext.HostingEnvironment;
        //            config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        //            //.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
        //            .AddEnvironmentVariables();
        //            if (args != null)
        //            {
        //                config.AddCommandLine(args);
        //            }
        //        })
        //    .ConfigureWebHostDefaults(webBuilder =>
        //    {
        //        webBuilder.UseSentry((context, configureOptions) =>
        //        {
        //            configureOptions.Dsn = "https://34387d0ebf484bc5a3dee949c9d1396f@o472576.ingest.sentry.io/5683544";
        //            configureOptions.Environment = context.HostingEnvironment.EnvironmentName;
        //            configureOptions.SendDefaultPii = true;

        //            configureOptions.MinimumEventLevel = Microsoft.Extensions.Logging.LogLevel.Warning;
        //            configureOptions.Debug = true;
        //            //var environmentName = context.HostingEnvironment.EnvironmentName.ToUpper();
        //            //switch (environmentName)
        //            //{
        //            //    case "DEVELOPMENT":
        //            //        configureOptions.Debug = true;
        //            //        break;
        //            //    default:
        //            //        configureOptions.Debug = true;
        //            //        break;
        //            //}
        //        });
        //        webBuilder.UseStartup<Startup>();
        //    });
        //}
    }
}
