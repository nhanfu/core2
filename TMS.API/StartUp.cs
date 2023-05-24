using Core.Exceptions;
using Core.Extensions;
using Core.ViewModels;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OData.Edm;
using Newtonsoft.Json;
using Polly;
using Polly.Extensions.Http;
using System.IO.Compression;
using System.Security.Claims;
using System.Text;
using TMS.API.BgService;
using TMS.API.Extensions;
using TMS.API.Models;
using TMS.API.Services;
using TMS.API.Websocket;

namespace TMS.API
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
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
                config.AddConfiguration(_configuration.GetSection("Logging"));
                config.AddDebug();
                config.AddEventSourceLogger();
            });
            services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
            services.AddResponseCompression(options =>
            {
                options.Providers.Add<GzipCompressionProvider>();
            });
            services.AddSingleton<EntityService>();
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
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Local;
            });
            services.AddDbContext<HistoryContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(_configuration.GetConnectionString($"History"), x => x.EnableRetryOnFailure());
#if DEBUG
                options.EnableSensitiveDataLogging();
#endif
            });
            services.AddDbContext<DBAccountantContext>((serviceProvider, options) =>
            {
                options.UseSqlServer(_configuration.GetConnectionString($"DBAccountant"), x => x.EnableRetryOnFailure());
#if DEBUG
                options.EnableSensitiveDataLogging();
#endif
            });
            services.AddDbContext<TMSContext>((serviceProvider, options) =>
            {
                string connectionStr = GetConnectionString(serviceProvider, _configuration, "Default");
                options.UseSqlServer(connectionStr, x => x.EnableRetryOnFailure());
#if DEBUG
                options.EnableSensitiveDataLogging();
#endif
            });
            services.AddOData();
            var tokenOptions = new TokenValidationParameters()
            {
                ValidIssuer = _configuration["Tokens:Issuer"],
                ValidAudience = _configuration["Tokens:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Tokens:Key"])),
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
            services.AddScoped<RealtimeService>();
            services.AddScoped<TaskService>();
            services.AddScoped<UserService>();
            services.AddScoped<VendorSvc>();
            services.AddScoped<TransportationService>();
            //
            services.AddHostedService<StatisticsService>();
            services.AddHostedService<KillService>();
        }

        static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
        }

        public static string GetConnectionString(IServiceProvider serviceProvider, IConfiguration _configuration, string system)
        {
            string tenantCode = GetTanentCode(serviceProvider);
            var connectionStr = _configuration.GetConnectionString($"{system}{tenantCode}")
                ?? _configuration.GetConnectionString(system);
            if (!tenantCode.IsNullOrWhiteSpace())
            {
                connectionStr = connectionStr.Replace($"softek_donga", $"softek_{tenantCode ?? "Softek"}");
            }
            return connectionStr;
        }

        public static TMSContext GetTMSContext(string conStr, bool subVendor = false)
        {
            if (conStr.IsNullOrWhiteSpace())
            {
                return null;
            }
            var vendorConnStr = conStr;
            if (subVendor)
            {
                var connStr = JsonConvert.DeserializeObject<List<VendorConnStrVM>>(conStr);
                vendorConnStr = connStr.FirstOrDefault(x => x.Name == "TMS").ConStr;
            }
            var builder = new DbContextOptionsBuilder<TMSContext>().UseSqlServer(vendorConnStr).Options;
            var context = new TMSContext(builder);
            return context;
        }

        private static string GetTanentCode(IServiceProvider serviceProvider)
        {
            var httpContext = serviceProvider.GetService<IHttpContextAccessor>();
            string tenantCode = null;
            if (httpContext?.HttpContext is not null)
            {
                var claim = httpContext.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.PrimaryGroupSid);
                if (claim is not null)
                {
                    tenantCode = claim.Value.ToUpper();
                }
                if (tenantCode.IsNullOrWhiteSpace())
                {
                    tenantCode = httpContext.HttpContext.Request.Query["t"].ToString();
                }
            }
            return tenantCode;
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, TMSContext tms, EntityService entity)
        {
            if (entity.Entities.Nothing())
            {
                entity.Entities = tms.Entity.ToDictionary(x => x.Id, x =>
                {
                    var res = new Core.Models.Entity();
                    res.CopyPropFrom(x);
                    return res;
                });
            }
            app.UseCors("MyPolicy");
            if (env.IsDevelopment())
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
            var serviceScopeFactory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            var serviceProvider = serviceScopeFactory.CreateScope().ServiceProvider;
            app.MapWebSocketManager("/task", serviceProvider.GetService<RealtimeService>());

            options.DefaultFileNames.Clear();
            options.DefaultFileNames.Add("index.html");
            app.UseDefaultFiles(options);
            app.UseStaticFiles();
            app.UseAuthentication();
            var model = GetEdmModel(app.ApplicationServices);
            app.UseMvc(builder =>
            {
                builder.EnableDependencyInjection();
                builder.MapODataServiceRoute("odataroute", "api", model);
                builder.Select().Expand().Filter().OrderBy().MaxTop(null).Count();
            });
            app.UseRouting();

        }

        private IEdmModel GetEdmModel(IServiceProvider applicationServices)
        {
            var builder = new ODataConventionModelBuilder(applicationServices);

            var userBuilder = builder.EntitySet<User>(nameof(User));
            userBuilder.EntityType.Ignore(x => x.Salt);
            userBuilder.EntityType.Ignore(x => x.Password);
            userBuilder.EntityType.Ignore(x => x.Recover);
            userBuilder.EntityType.Ignore(x => x.LastFailedLogin);
            userBuilder.EntityType.Ignore(x => x.LoginFailedCount);
            userBuilder.EntityType.Ignore(x => x.LastLogin);
            return builder.GetEdmModel();
        }
    }
}
