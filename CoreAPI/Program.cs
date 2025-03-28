using Core.Extensions;
using Core.Middlewares;
using Core.Services;
using CoreAPI.BgService;
using CoreAPI.Services;
using CoreAPI.Services.Sql;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
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
       .UseSqlServerStorage(conf.GetConnectionString("logistics"), new SqlServerStorageOptions
       {
           QueuePollInterval = TimeSpan.FromSeconds(15), // Kiểm tra job mới mỗi 15s
           JobExpirationCheckInterval = TimeSpan.FromHours(1)
       }));
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
    ValidateIssuer = true,
    ValidateAudience = true,
    ValidateLifetime = true,
    ValidateIssuerSigningKey = true,
    ValidIssuer = conf["Tokens:Issuer"],
    ValidAudience = conf["Tokens:Issuer"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(conf["Tokens:Key"])),
    ClockSkew = TimeSpan.Zero
};
services.AddSingleton(tokenOptions);
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = conf["Tokens:Issuer"];
    options.Audience = conf["Tokens:Issuer"];
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = tokenOptions;
});
services.AddDistributedMemoryCache();
services.AddHttpContextAccessor();
services.AddScoped<SqlServerProvider>();
services.AddScoped<DuckDbProvider>();
services.AddScoped<ISqlProvider, SqlServerProvider>();
services.AddScoped<WebSocketService>();
services.AddScoped<UserService>();
services.AddScoped<SendMailService>();
services.AddScoped<PdfService>();
services.AddScoped<ExcelService>();
services.AddScoped<OpenAIHttpClientService>();
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
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseMvc();
app.Run();