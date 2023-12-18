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

            // var builder = WebApplication.CreateBuilder(args);

            // // Add services to the container.
            // builder.Services.AddRazorPages();

            // var app = builder.Build();

            // // Configure the HTTP request pipeline.
            // if (!app.Environment.IsDevelopment())
            // {
            //     app.UseExceptionHandler("/Error");
            //     // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            //     app.UseHsts();
            // }

            // app.UseHttpsRedirection();
            // app.UseStaticFiles();

            // app.UseRouting();

            // app.UseAuthorization();

            // app.MapRazorPages();

            // app.Run();
        }
    }
}
