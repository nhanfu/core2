using Core.Extensions;
using Core.Middlewares;
using Core.Services;
using CoreAPI.BgService;
using CoreAPI.Services;
using CoreAPI.Services.Sql;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var conf = builder.Configuration;
services.AddHttpClient();
services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
{
    builder.AllowAnyOrigin()
           .AllowAnyMethod()
           .AllowAnyHeader();
}));
services.Configure<IISServerOptions>(options =>
{
    options.AutomaticAuthentication = false;
});
services.AddDistributedMemoryCache();
services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConfiguration(conf.GetSection("Logging"));
    config.AddDebug();
    config.AddEventSourceLogger();
});
services.AddHangfire(configuration => configuration
       .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
       .UseSimpleAssemblyNameTypeSerializer()
       .UseRecommendedSerializerSettings()
       .UseSqlServerStorage(conf.GetConnectionString("Default")));

// Add the processing server as IHostedService
services.AddHangfireServer();
services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
});
services.AddSingleton<ConnectionManager>();
services.AddMvc(options =>
{
    options.EnableEndpointRouting = false;
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new IgnoreNullOrEmptyEnumResolver();
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    options.SerializerSettings.NullValueHandling = NullValueHandling.Include;

    options.SerializerSettings.Converters.Add(new DateParser());
    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
    options.SerializerSettings.DateParseHandling = DateParseHandling.DateTime;
    options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
});
var tokenOptions = new TokenValidationParameters()
{
    ValidIssuer = conf["Tokens:Issuer"],
    ValidAudience = conf["Tokens:Issuer"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(conf["Tokens:Key"])),
    ClockSkew = TimeSpan.Zero
};
services.AddSingleton(tokenOptions);
services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(cfg =>
{
    cfg.RequireHttpsMetadata = false;
    cfg.SaveToken = true;
    cfg.TokenValidationParameters = tokenOptions;

});
services.AddDistributedMemoryCache();
services.AddHttpContextAccessor();

// the instance created for each request
services.AddScoped<SqlServerProvider>();
services.AddScoped<DuckDbProvider>();
services.AddScoped<ISqlProvider, SqlServerProvider>();
services.AddScoped<WebSocketService>();
services.AddScoped<UserService>();
services.AddScoped<SendMailService>();
var app = builder.Build();
app.UseHangfireDashboard();
#if !DEBUG
RecurringJob.AddOrUpdate<DailyFunction>("CoreAPI.BgService.DailyFunction",
x => x.StatisticsProcesses(), Cron.Daily(06, 00), new RecurringJobOptions()
{
    TimeZone = TimeZoneInfo.Local,
});
RecurringJob.AddOrUpdate<CustomerFunction>("CoreAPI.BgService.CustomerFunction",
x => x.StatisticsProcesses(), Cron.Daily(00, 00), new RecurringJobOptions()
{
    TimeZone = TimeZoneInfo.Local,
});
#endif
app.UseCors("MyPolicy");
app.UseAuthentication();
app.UseWebSockets();
app.UseMiddleware<GlobalMiddleware>();
app.UseMiddleware<LoadBalaceMiddleware>();
app.UseTaskSocket();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseMvc();
app.UseRouting();
app.Run();