using Microsoft.AspNetCore;

namespace Core
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
