using Core.Enums;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Core.Extensions
{
    public static class ReflectionExt
    {
        public static bool IsSimple(this Type type)
        {
            if (type is null)
            {
                throw new ArgumentNullException($"{nameof(type)} is null");
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return IsSimple(type.GetGenericArguments()[0]);
            }
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(DateTime))
              || type.Equals(typeof(DateTimeOffset))
              || type.Equals(typeof(TimeSpan))
              || type.Equals(typeof(decimal));
        }

        public static bool IsDate(this Type type)
        {
            return type.Equals(typeof(DateTime)) || type.Equals(typeof(DateTime?)) || type.Equals(typeof(DateTimeOffset)) || type.Equals(typeof(DateTimeOffset?));
        }

        public static bool IsNumber(this Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return IsNumber(type.GetGenericArguments()[0]);
            }
            return type == typeof(sbyte)
                    || type == typeof(byte)
                    || type == typeof(short)
                    || type == typeof(ushort)
                    || type == typeof(int)
                    || type == typeof(uint)
                    || type == typeof(int)
                    || type == typeof(ulong)
                    || type == typeof(float)
                    || type == typeof(double)
                    || type == typeof(decimal);
        }

        public static bool IsDecimal(this Type type)
        {
            return type == typeof(decimal) || type == typeof(decimal?);
        }

        public static bool IsInt32(this Type type)
        {
            return type == typeof(int) || type == typeof(int?);
        }

        public static bool IsBool(this Type type)
        {
            return type == typeof(bool) || type == typeof(bool?);
        }

        public static void CopyPropFrom(this object target, object source, params string[] ignoreFields)
        {
            CopyPropFromInternal(target, source, new HashSet<int>(), 0, 0, ignoreFields);
        }

        public static void CopyPropFromAct(this object target, object source)
        {
            if (source is null || target is null)
            {
                return;
            }

            var targetType = target.GetType();
            if (targetType.IsSimple())
            {
                return;
            }
            var targetProps = targetType.GetProperties().Where(x => x.CanRead && x.CanWrite);
            foreach (var targetProp in targetProps)
            {
                var value = source.GetPropValue(targetProp.Name);
                if (targetProp.PropertyType.IsDecimal() && value != null && !value.GetType().IsDecimal())
                {
                    target.SetPropValue(targetProp.Name, Convert.ToDecimal(value));
                }
                else if (targetProp.PropertyType.IsDate() && value != null && !value.GetType().IsDate())
                {
                    target.SetPropValue(targetProp.Name, Convert.ToDateTime(value));
                }
                else if (targetProp.PropertyType.IsSimple())
                {
                    target.SetPropValue(targetProp.Name, value);
                }
                else
                {
                    target.GetPropValue(targetProp.Name).CopyPropFromAct(value);
                }
            }
        }

        public static void CopyPropFrom(this object target, object source, int maxLevel = 0, params string[] ignoreFields)
        {
            CopyPropFromInternal(target, source, new HashSet<int>(), 0, maxLevel, ignoreFields);
        }

        public static void CopyPropFromInternal(this object target, object source, HashSet<int> visited, int currentLevel = 0, int maxLevel = 0, params string[] ignoreFields)
        {
            var shouldProcessNested = maxLevel == 0 || currentLevel < maxLevel;
            if (source is null || target is null || !shouldProcessNested)
            {
                return;
            }

            var targetType = target.GetType();
            if (targetType.IsSimple())
            {
                return;
            }

            var hash = source.GetHashCode();
            if (visited.Contains(hash))
            {
                return;
            }

            visited.Add(hash);
            if (typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                var sourceEnumerator = (source as IEnumerable).GetEnumerator();
                var targetEnumerator = (target as IEnumerable).GetEnumerator();
                while (sourceEnumerator.MoveNext() && targetEnumerator.MoveNext())
                {
                    targetEnumerator.Current.CopyPropFromInternal(sourceEnumerator.Current, visited, currentLevel + 1, maxLevel, ignoreFields);
                }
                return;
            }
            var targetProps = targetType.GetProperties().Where(x => x.CanRead && x.CanWrite);
            foreach (var targetProp in targetProps)
            {
                if (ignoreFields != null && ignoreFields.Contains(targetProp.Name))
                {
                    continue;
                }

                var value = source.GetPropValue(targetProp.Name);
                if (targetProp.PropertyType.IsDecimal() && value != null && !value.GetType().IsDecimal())
                {
                    target.SetPropValue(targetProp.Name, Convert.ToDecimal(value));
                }
                else if (targetProp.PropertyType.IsDate() && value != null && !value.GetType().IsDate())
                {
                    target.SetPropValue(targetProp.Name, Convert.ToDateTime(value));
                }
                else if (targetProp.PropertyType.IsSimple())
                {
                    target.SetPropValue(targetProp.Name, value);
                }
                else
                {
                    target.GetPropValue(targetProp.Name).CopyPropFromInternal(value, visited, currentLevel + 1, maxLevel, ignoreFields);
                }
            }
        }

        public static void ClearReferences(this object obj, bool clearInnerEnum = false, HashSet<object> visited = null)
        {
            if (visited is null)
            {
                visited = new HashSet<object>();
            }
            if (obj is null || visited.Contains(obj) || obj.GetType().IsSimple())
            {
                return;
            }
            visited.Add(obj);

            if (obj is IEnumerable list)
            {
                foreach (var item in list)
                {
                    item.ClearReferences(clearInnerEnum, visited);
                }
            }
            else
            {
                ClearRefInternal(obj, clearInnerEnum, visited);
            }
        }

        private static void ClearRefInternal(object obj, bool clearInnerEnum, HashSet<object> visited)
        {
            var props = obj.GetType().GetProperties().Where(x => x.CanWrite && x.CanWrite);
            foreach (var prop in props)
            {
                var refProp = prop.Name.SubStrIndex(0, prop.Name.Length - 2);
                var enumerable = prop.GetValue(obj) as IEnumerable;
                if (!prop.PropertyType.IsSimple() && enumerable is null)
                {
                    prop.SetValue(obj, null);
                }
#if !NETCOREAPP
                else if (prop.PropertyType.IsSimple() && obj[refProp] != null && !obj[refProp].GetType().IsSimple())
                {
                    obj[refProp] = null;
                }
#endif

                if (prop.PropertyType != typeof(string) && enumerable != null)
                {
                    if (clearInnerEnum)
                    {
                        prop.SetValue(obj, null);
                        continue;
                    }
                    foreach (var item in enumerable)
                    {
                        item.ClearReferences(clearInnerEnum, visited);
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0019:Use pattern matching", Justification = "<Pending>")]
        public static string GetEnumDescription(this Enum value, Type enumType = null)
        {
            enumType = enumType ?? value.GetType();
            var fi = enumType.GetField(value.ToString());
            if (fi is null)
            {
                return string.Empty;
            }

            var attributes = fi.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
            if (attributes != null && attributes.Any())
            {
                return attributes.First().Description;
            }
            return value.ToString();
        }

        public static object DeepCopy(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            Type type = obj.GetType();

            if (type.IsSimple())
            {
                return obj;
            }
            else if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                object copied = Activator.CreateInstance(type);
                var childrenType = type.GetGenericArguments()[0];
                var enumerator = (obj as IEnumerable).GetEnumerator();
                var addMethod = copied.GetType().GetMethod("Add", BindingFlags.Public);
                while (enumerator.MoveNext())
                {
                    addMethod.MakeGenericMethod(childrenType)?.Invoke(copied, new object[] { DeepCopy(enumerator.Current) });
                }
                return copied;
            }
            else if (type.IsClass)
            {
                object toret = Activator.CreateInstance(obj.GetType());
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (FieldInfo field in fields)
                {
                    object fieldValue = field.GetValue(obj);
                    if (fieldValue == null)
                    {
                        continue;
                    }

                    field.SetValue(toret, DeepCopy(fieldValue));
                }
                return toret;
            }
            return null;
        }

        public const string IdField = "Id";
        public const string StatusIdField = "StatusId";
        public const string FreightStateId = "FreightStateId";
        public static List<T> CopyRowWithoutId<T>(List<T> selectedRows)
        {
            var copiedRows = selectedRows.Select(x =>
            {
                var res = (T)DeepCopy(x);
                res.SetPropValue(IdField, -Math.Abs(res.GetHashCode()));
                ProcessObjectRecursive(res, obj =>
                {
                    var id = obj.GetPropValue(IdField) as int?;
                    if (id.HasValue && id.Value > 0)
                    {
                        obj.SetPropValue(IdField, 0);
                    }

                    var status = obj.GetPropValue(StatusIdField) as int?;
                    if (status.HasValue)
                    {
                        obj.SetPropValue(StatusIdField, (int)ApprovalStatusEnum.New);
                    }
                    var freightState = obj.GetPropValue(FreightStateId) as int?;
                    if (status.HasValue)
                    {
                        obj.SetPropValue(FreightStateId, 1);
                    }
                });
                res.ClearReferences();
                return res;
            }).ToList();
            return copiedRows;
        }

        public static void ProcessObjectRecursive(object obj, Action<object> action, HashSet<object> visited = null)
        {
            if (obj is null || obj.GetType().IsSimple())
            {
                return;
            }

            if (visited == null)
            {
                visited = new HashSet<object>();
            }

            if (visited.Contains(obj))
            {
                return;
            }

            visited.Add(obj);
            if (obj is IEnumerable list)
            {
                try
                {
                    var enumerator = list.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        ProcessObjectRecursive(enumerator.Current, action, visited);
                    }
                }
                catch
                {
                }
                return;
            }
            action(obj);
            var childrenList = obj.GetType().GetProperties().Where(x => !x.PropertyType.IsSimple() && typeof(IEnumerable).IsAssignableFrom(x.PropertyType));
            foreach (var child in childrenList)
            {
                ProcessObjectRecursive(child.GetValue(obj), action, visited);
            }
        }

        public static Dictionary<Key, T> ToDictionaryDistinct<T, Key>(this IEnumerable<T> source, Func<T, Key> keySelector)
        {
            if (source is null)
            {
                return null;
            }

            return source.GroupBy(keySelector).Select(g => g.First()).ToDictionary(keySelector);
        }
    }
}
