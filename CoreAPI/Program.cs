using Core.Extensions;
using Core.Services;
using Core.Websocket;
using CoreAPI.Middlewares;
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
services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
});
services.AddWebSocketManager();
services.AddMvc(options =>
{
    options.EnableEndpointRouting = false;
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new IgnoreNullOrEmptyEnumResolver();
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;

    options.SerializerSettings.Converters.Add(new DateParser());
    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
    options.SerializerSettings.DateParseHandling = DateParseHandling.DateTimeOffset;
    options.SerializerSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
});
services.AddHangfire(configuration => configuration.UseSqlServerStorage(conf.GetConnectionString($"Log")));
services.AddHangfireServer();
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
services.AddScoped<WebSocketService>();
services.AddScoped<TaskService>();
services.AddScoped<UserService>();

var app = builder.Build();

app.UseCors("MyPolicy");
app.UseWebSockets();
app.UseMiddleware<ExceptionMiddleware>();
app.UseMiddleware<LoadBalaceMiddleware>();
app.UseSocketHandler();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseHangfireDashboard();
app.UseAuthentication();
app.UseMvc();
app.UseRouting();

app.Run();