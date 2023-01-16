using Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Linq;
using System.Reflection;
using TMS.API.Websocket;

namespace TMS.API.Extensions
{
    public static class ServerExt
    {
        public static IApplicationBuilder MapWebSocketManager(
            this IApplicationBuilder app, PathString path, WebSocketHandler handler)
        {
            return app.Map(path, (_app) => _app.UseMiddleware<WebSocketManagerMiddleware>(handler));
        }

        public static IServiceCollection AddWebSocketManager(this IServiceCollection services)
        {
            services.AddSingleton<ConnectionManager>();

            foreach (var type in Assembly.GetExecutingAssembly().ExportedTypes)
            {
                if (type.GetTypeInfo().BaseType == typeof(WebSocketHandler))
                {
                    services.AddSingleton(type);
                }
            }
            return services;
        }

        public static string ToJson(this object value) => JsonConvert.SerializeObject(value, new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        });

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
