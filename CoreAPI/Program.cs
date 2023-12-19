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
builder.AddServiceDefaults();
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "redis";
});
var _conf = builder.Configuration;
var services = builder.Services;

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
services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddConfiguration(_conf.GetSection("Logging"));
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
services.AddHangfire(configuration => configuration.UseSqlServerStorage(_conf.GetConnectionString($"Log")));
var tokenOptions = new TokenValidationParameters()
{
    ValidIssuer = _conf["Tokens:Issuer"],
    ValidAudience = _conf["Tokens:Issuer"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_conf["Tokens:Key"])),
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

app.MapDefaultEndpoints();

app.UseCors("MyPolicy");
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseResponseCompression();
app.UseStaticFiles();
app.UseHangfireDashboard();
app.UseHangfireServer();
app.UseMiddleware<ExceptionMiddleware>();
UseSocket(app);
app.UseAuthentication();
app.UseMvc();
app.UseRouting();

app.Run();

void UseSocket(IApplicationBuilder app)
{
    app.UseWebSockets();
    var serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
    var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
    app.Map("/task", app => app.UseMiddleware<WebSocketManagerMiddleware>(serviceProvider.GetService<WebSocketService>()));
}