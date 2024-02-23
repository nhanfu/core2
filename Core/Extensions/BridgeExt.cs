using Bridge.Html5;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Extensions
{
    public static class BridgeExt
    {
        public static void SetComplexPropValue(this object obj, string propName, object value)
        {
            if (obj == null || string.IsNullOrWhiteSpace(propName))
            {
                return;
            }

            var hierarchy = propName.Split('.');
            if (hierarchy.Length == 0)
            {
                return;
            }

            if (hierarchy.Length == 1)
            {
                obj.SetPropValue(propName, value);
                return;
            }
            var leaf = obj;
            for (var i = 0; i < hierarchy.Length - 1; i++)
            {
                if (leaf == null)
                {
                    return;
                }

                var key = hierarchy[i];
                leaf = leaf.GetPropValue(key);
            }
            if (leaf == null)
            {
                return;
            }

            leaf.SetPropValue(hierarchy[hierarchy.Length - 1].ToString(), value);
        }

        public static string CustomFormat(this DateTime? date, string format = "dd/MM/yyyy")
        {
            if (date is null)
            {
                return string.Empty;
            }

            var dateTime = DateTime.Parse(date.As<string>());
            return dateTime.ToString(format);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0034:Simplify 'default' expression", Justification = "<Pending>")]
        public static T CastProp<T>(this object obj)
        {
            if (obj is null)
            {
                return default(T);
            }

            T res;
            res = (T)Activator.CreateInstance(typeof(T));
            res.CopyPropFrom(obj);
            return res;
        }

        public static void SetPropValue(this object instance, string propertyName, object value)
        {
            if (propertyName.IsNullOrWhiteSpace()) return;
            var type = instance.GetType();
            var prop = type.GetProperty(propertyName);
            if (prop != null && type != null && prop.CanWrite)
            {
                prop.SetValue(instance, value, null);
            }
            else if (prop == null && instance != null)
            {
                instance[propertyName] = value;
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
                return (obj.HasOwnProperty(propName) || obj.GetType().GetProperty(propName) != null, obj[propName]);
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

                res = res[key];
            }
            if (res == null)
            {
                return (false, null);
            }

            var hasProp = res.HasOwnProperty(lastField) || res.GetType().GetProperty(lastField) != null;
            return (hasProp, res[lastField]);
        }

        /// <summary>
        /// Get complex property value and type
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propName"></param>
        /// <returns>T1: The type of the complex property<br /> T2: The complex propperty value</returns>
        public static Type GetComplexPropType(this Type type, string propName, object obj = null)
        {
            if (type is null || string.IsNullOrWhiteSpace(propName))
            {
                return null;
            }

            obj = obj ?? Activator.CreateInstance(type);
            var hierarchy = propName.Split('.');
            if (hierarchy.Length == 0)
            {
                return null;
            }

            if (hierarchy.Length == 1)
            {
                return GetObjType(propName, obj);
            }
            var lastField = hierarchy.LastOrDefault();
            hierarchy = hierarchy.Take(hierarchy.Length - 1).ToArray();
            foreach (var key in hierarchy)
            {
                if (obj == null)
                {
                    return null;
                }

                obj = obj[key];
            }
            if (obj is null)
            {
                return null;
            }

            return GetObjType(lastField, obj);
        }

        private static Type GetObjType(string propName, object obj)
        {
            if (obj.HasOwnProperty(propName))
            {
                var value = obj.GetPropValue(propName);
                return value?.GetType();
            }
            else if (obj.GetType().GetProperty(propName) != null)
            {
                return obj.GetType().GetProperty(propName)?.PropertyType;
            }

            return null;
        }

        public static IEnumerable<T> DistinctBy<T, Key>(this IEnumerable<T> source, Func<T, Key> keySelector)
        {
            if (source is null)
            {
                return null;
            }

            return source.GroupBy(keySelector).Select(g => g.First());
        }

        public static decimal TextWidth(this string text, string font)
        {
            var div = Document.CreateElement("div");
            div.TextContent = text;
            div.Style.CssText = $"position: absolute; float: left; white-space: nowrap; visibility: hidden, font: {font}";
            Document.Body.AppendChild(div);
            var css = Window.GetComputedStyle(div, "width");
            return css.Width.Replace("px", "").TryParse<decimal>() ?? 0;
        }

        public static void Swap<T>(List<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }
    }
}
