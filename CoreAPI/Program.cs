using Core.Extensions;
using Core.Middlewares;
using Core.Services;
using CoreAPI.Services;
using CoreAPI.Services.Interfaces;
using CoreAPI.Services.Sql;
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
    config.AddConsole();
    config.AddEventSourceLogger();
});
services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>();
});
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
services.AddScoped<PostgreSqlProvider>();
services.AddScoped<ISqlProvider, PostgreSqlProvider>();
services.AddScoped<IPatchService, PatchService>();
services.AddScoped<IQueryService, QueryService>();
services.AddScoped<IFileService, FileService>();
services.AddScoped<IMetadataService, MetadataService>();
services.AddScoped<UserService>();
services.AddScoped<AuthService>();
services.AddScoped<SendMailService>();
services.AddScoped<PdfService>();
services.AddScoped<ExcelService>();
services.AddScoped<OpenAIHttpClientService>();
var app = builder.Build();
app.UseCors("MyPolicy");
app.UseAuthentication();
app.UseWebSockets();
app.UseMiddleware<GlobalMiddleware>();
app.UseResponseCompression();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseMvc();
app.Run();