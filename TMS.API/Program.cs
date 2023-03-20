using Core.Exceptions;
using Core.Extensions;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Text;
using TMS.API.BgService;
using TMS.API.Extensions;
using TMS.API.Models;
using TMS.API.Services;
using TMS.API.Websocket;
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddCors(o => o.AddPolicy("MyPolicy", builder =>
{
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
}));
builder.Services.Configure<IISServerOptions>(options =>
{
    options.AutomaticAuthentication = false;
});
builder.Services.AddLogging(config =>
{
    config.ClearProviders();
    config.AddDebug();
    config.AddEventSourceLogger();
});
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.AddSingleton<EntityService>();
builder.Services.AddWebSocketManager();
builder.Services.AddMvc(options =>
{
    options.EnableEndpointRouting = false;
})
.AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ContractResolver = new IgnoreNullOrEmptyEnumResolver();
    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
});
builder.Services.AddDbContext<HistoryContext>((serviceProvider, options) =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString($"History"), x => x.EnableRetryOnFailure());
#if DEBUG
    options.EnableSensitiveDataLogging();
#endif
});
builder.Services.AddDbContext<TMSContext>((serviceProvider, options) =>
{
    string connectionStr = builder.Configuration.GetConnectionString($"Default");
    options.UseSqlServer(connectionStr, x => x.EnableRetryOnFailure());
#if DEBUG
    options.EnableSensitiveDataLogging();
#endif
});
builder.Services.AddOData();
var tokenOptions = new TokenValidationParameters()
{
    ValidIssuer = builder.Configuration["Tokens:Issuer"],
    ValidAudience = builder.Configuration["Tokens:Issuer"],
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Tokens:Key"])),
    ClockSkew = TimeSpan.Zero
};
builder.Services.AddSingleton(tokenOptions);
builder.Services.AddAuthentication(x =>
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
builder.Services.AddDistributedMemoryCache();
builder.Services.AddHttpContextAccessor();

// the instance created for each request
builder.Services.AddScoped<RealtimeService>();
builder.Services.AddScoped<TaskService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<VendorSvc>();
//
builder.Services.AddHostedService<StatisticsService>();


var app = builder.Build();

app.UseCors("MyPolicy");
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseHttpStatusCodeExceptionMiddleware();
}
else
{
    app.UseHttpStatusCodeExceptionMiddleware();
    app.UseHsts();
}
app.UseHttpsRedirection();
var options = new DefaultFilesOptions();
app.UseResponseCompression();
app.UseWebSockets();
var serviceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
app.MapWebSocketManager("/task", serviceProvider.GetService<RealtimeService>());
options.DefaultFileNames.Clear();
app.UseDefaultFiles(options);
app.UseStaticFiles();
app.UseAuthentication();
var builder1 = new ODataConventionModelBuilder(serviceProvider);
var userBuilder = builder1.EntitySet<User>(nameof(User));
userBuilder.EntityType.Ignore(x => x.Salt);
userBuilder.EntityType.Ignore(x => x.Password);
userBuilder.EntityType.Ignore(x => x.Recover);
userBuilder.EntityType.Ignore(x => x.LastFailedLogin);
userBuilder.EntityType.Ignore(x => x.LoginFailedCount);
userBuilder.EntityType.Ignore(x => x.LastLogin);
var rs = builder1.GetEdmModel();
app.UseMvc(builder =>
{
    builder.EnableDependencyInjection();
    builder.MapODataServiceRoute("odataroute", "api", rs);
    builder.Select().Expand().Filter().OrderBy().MaxTop(null).Count();
});
app.UseRouting();
app.MapRazorPages();
app.Run();
