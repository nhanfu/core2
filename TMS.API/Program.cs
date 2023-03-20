using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;


namespace TMS.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var webConfig = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();
            var host = WebHost.CreateDefaultBuilder(args)
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConsole();
                })
                .UseConfiguration(webConfig);
            host.UseContentRoot(Directory.GetCurrentDirectory())
                .UseStartup<Startup>();
            host.Build().Run();
        }
    }
}
