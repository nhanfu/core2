using Core.Websocket;
using Newtonsoft.Json;
using Org.BouncyCastle.Utilities.Net;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Core.Extensions
{
    public static class ServerExt
    {
        public static IApplicationBuilder UseSocketHandler(this IApplicationBuilder app, IServiceProvider provider, string prefix = "/task")
        {
            app.Map(prefix, app => app.UseMiddleware<WebSocketManagerMiddleware>(provider.GetKeyedService<WebSocketService>(prefix)));
            return app;
        }

        public static IApplicationBuilder UseClusterSocket(this IApplicationBuilder app)
        {
            var factory = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            var provider = factory.CreateScope().ServiceProvider;
            var conf = provider.GetService<IConfiguration>();
            var isBalancer = conf.GetSection("Role").Get<string>() == Utils.Balancer;
            if (isBalancer)
            {
                app.UseSocketHandler(provider, "/clusters");
            }
            else
            {
                app.UseSocketHandler(provider, "/clusters");
                app.UseSocketHandler(provider, "/task");
            }

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

        private static readonly JsonSerializerSettings settings = new()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
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
                return default;
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
