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
        public static void Action<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source.Nothing() || action is null)
            {
                return;
            }

            foreach (var item in source)
            {
                action(item);
            }
        }

        public static IEnumerable<T> SelectForEach<T>(this IEnumerable<T> source, Action<T> action)
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

        public static IEnumerable<T> SelectForEach<T>(this IEnumerable<T> source, Action<T, int> action)
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

        public static IEnumerable<K> SelectForEach<T, K>(this IEnumerable<T> source, Func<T, int, K> mapper)
        {
            if (source == null || mapper is null)
            {
                throw new ArgumentNullException("source or mapper is null");
            }

            int index = 0;
            foreach (var item in source)
            {
                yield return mapper(item, index);
                index++;
            }
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

        public static IEnumerable<T> WhereNot<T>(this IEnumerable<T> source, Predicate<T> predicate)
        {
            if (source.Nothing() || predicate is null)
            {
                yield break;
            }

            foreach (var item in source)
            {
                if (!predicate(item)) yield return item;
            }
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
                    Id = ((int)value).ToString(),
                    Name = val.ToString(),
                    Description = desc,
                    Active = true
                });
            }
            return res;
        }

        public static string CombineStrings(this IEnumerable<string> source, string combinator = ",")
        {
            if (combinator is null || source is null)
            {
                return string.Empty;
            }
            return source.Select(x => $"'{x}'").Combine(combinator);
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
