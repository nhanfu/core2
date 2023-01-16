using Core.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Extensions
{
    public static class IEnumerableExtensions
    {
        public static IEnumerable<object> GetSourceByType(this IEnumerable<IEnumerable<object>> sources, Type type)
        {
            if (sources is null)
            {
                return null;
            }

            return sources.FirstOrDefault(x => x.GetType().GetGenericArguments()[0] == type);
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source.Nothing() || action is null)
            {
                return source;
            }

            foreach (var item in source)
            {
                action(item);
            }
            return source;
        }

        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source.Nothing() || action is null)
            {
                return source;
            }

            int index = 0;
            foreach (var item in source)
            {
                action(item, index);
                index++;
            }
            return source;
        }

        public static async Task<IEnumerable<T>> ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> action)
        {
            if (source.Nothing() || action is null)
            {
                return source;
            }

            var tasks = source.Select(action);
            await Task.WhenAll(tasks);
            return source;
        }

        public static bool Nothing<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }

        public static bool HasElementAndAll<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            if (source == null || !source.Any())
            {
                return false;
            }

            return source.All(predicate);
        }

        public static (T, int) FindItemAndIndex<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var index = -1;
            foreach (var item in source)
            {
                index++;
                if (predicate(item))
                {
                    return (item, index);
                }
            }
            return (default(T), -1);
        }

        public static int IndexOf<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var index = -1;
            foreach (var item in source)
            {
                index++;
                if (predicate(item))
                {
                    return index;
                }
            }
            return -1;
        }

        public static bool HasElement<T>(this IEnumerable<T> source)
        {
            return source != null && source.Any();
        }

        public static IEnumerable<T> Except<T>(this IEnumerable<T> source, T ele)
        {
            if (source is null)
            {
                return source;
            }

            return source.Where(x => !x.Equals(ele));
        }

        public static List<object> ToEntity<T>() where T : Enum
        {
            var enumType = typeof(T);
            var values = Enum.GetValues(enumType);
            var res = new List<object>();
            foreach (var value in values)
            {
                var val = Enum.Parse(enumType, value.ToString());
                var desc = val.ToString();
                var fi = enumType.GetField(desc);
                var attributes = fi?.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
                if (attributes.HasElement())
                {
                    desc = attributes.First().Description;
                }
                res.Add(new Entity
                {
                    Id = (int)value,
                    Name = val.ToString(),
                    Description = desc,
                    Active = true
                });
            }
            return res;
        }

        public static string Combine<T>(this IEnumerable<T> source, string combinator = ",")
        {
            if (combinator is null || source is null)
            {
                return string.Empty;
            }
            return string.Join(combinator, source);
        }

        public static string Combine<T, K>(this IEnumerable<T> source, Func<T, K> mapper, string combinator = ",")
        {
            if (mapper is null || source is null)
            {
                return string.Empty;
            }
            return source.Select(mapper).Combine(combinator);
        }

        public static bool All(this IEnumerable<bool> source)
        {
            return source.All(x => x);
        }

        public static bool Any(this IEnumerable<bool> source)
        {
            return source.Any(x => x);
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source) where T : struct
        {
            if (source is null)
            {
                return null;
            }

            return source.Where(x => x.HasValue).Select(g => g.Value);
        }

        public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source) where T : class
        {
            if (source is null)
            {
                return null;
            }

            return source.Where(x => x != null);
        }

        public static IEnumerable<T> Flattern<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> getChildren)
        {
            if (source.Nothing())
            {
                return source;
            }

            var firstLevel = source.Select(x => getChildren(x)).Where(x => x != null).SelectMany(x => x);
            if (firstLevel.Nothing())
            {
                return source;
            }
            return source.Concat(firstLevel.Flattern(getChildren));
        }
    }
}
