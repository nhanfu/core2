using Core.Websocket;
using CoreAPI.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Reflection;

namespace Core.Extensions
{
    public static class ServerExt
    {
        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddSingleton<ConnectionManager>();

            foreach (var type in Assembly.GetExecutingAssembly().ExportedTypes)
            {
                if (type.GetTypeInfo().BaseType == typeof(WebSocketService))
                {
                    services.AddSingleton(type);
                }
            }
            return services;
        }

        public static IApplicationBuilder UseSocketHandler(this IApplicationBuilder app, string prefix = "/task")
        {
            var factory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            var provider = factory.CreateScope().ServiceProvider;
            var config = provider.GetService<IConfiguration>();
            if (config.GetSection("Role").Get<string>() == Utils.Balancer) return app;
            app.Map(prefix, app => app.UseMiddleware<WebSocketManagerMiddleware>(provider.GetService<WebSocketService>()));
            return app;
        }

        public static WebApplication UseClusterAPI(this WebApplication app)
        {
            if (app.Configuration.GetSection("Role").Get<string>() != Utils.Balancer) return app;
            app.MapPost("/api/cluster/add", [Authorize] ([FromBody] Node node) =>
            {
                Cluster.Data.Nodes.Add(node);
            });
            app.MapPost("/api/cluster/remove", [Authorize] ([FromBody] Node node) =>
            {
                var node2Remove = Cluster.Data.Nodes.FirstOrDefault(x => x.Host == node.Host && x.Port == node.Port && x.Scheme == node.Scheme);
                Cluster.Data.Nodes.Remove(node2Remove);
            });
            return app;
        }

        public static object GetPropValue(this object obj, string propName)
        {
            if (obj is null)
            {
                return null;
            }

            var prop = obj.GetType().GetProperty(propName);
            return prop?.GetValue(obj);
        }

        public static void SetPropValue(this object instance, string propertyName, object value)
        {
            var type = instance.GetType();
            var prop = type.GetProperty(propertyName);
            prop?.SetValue(instance, value, null);
        }

        public static void SetReadonlyPropValue(this object instance, string propertyName, object value)
        {
            var type = instance.GetType();
            var prop = type.BaseType.GetProperty(propertyName);
            prop?.SetValue(instance, value, null);
        }
        private static JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        public static string ToJson(this object value) => JsonConvert.SerializeObject(value, settings);

        public static T TryParse<T>(this string value)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(value);
            }
            catch
            {
                return default(T);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propName"></param>
        /// <returns>T1: The object has the complex key <br /> T2: The complex propperty value</returns>
        public static (bool, object) GetComplexProp(this object obj, string propName)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propName))
            {
                return (false, null);
            }

            var hierarchy = propName.Split('.');
            if (hierarchy.Length == 0)
            {
                return (false, null);
            }

            if (hierarchy.Length == 1)
            {
                return (obj.GetType().GetProperty(propName) != null, obj.GetPropValue(propName));
            }

            var lastField = hierarchy.LastOrDefault();
            hierarchy = hierarchy.Take(hierarchy.Length - 1).ToArray();
            var res = obj;
            foreach (var key in hierarchy)
            {
                if (res == null)
                {
                    return (false, null);
                }

                res = res.GetPropValue(key);
            }
            return (res != null && res.GetType().GetProperty(lastField) != null, res.GetPropValue(lastField));
        }
    }
}
