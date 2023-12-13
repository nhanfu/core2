using Core.ViewModels;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Core.Extensions
{
    public static class ReflectionExt
    {
        public static bool IsSimple(this Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

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

        public static object DeepCopy(object obj, string path = null)
        {
            if (obj == null)
            {
                return null;
            }

            Type type = obj.GetType();
            var modelName = obj.GetPropValue("ModelName");
            if (modelName != null)
            {
                type = Type.GetType(path + modelName);
            }
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
                object toret;
                if (modelName != null)
                {
                    toret = Activator.CreateInstance(Type.GetType(path + modelName));
                }
                else
                {
                    toret = Activator.CreateInstance(obj.GetType());
                }
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

        public static T MapTo<T>(this Dictionary<string, object> keyVal) where T : class 
        {
            if (keyVal is null) return null;
            var instance = (T)Activator.CreateInstance(typeof(T));
            var props = typeof(T).GetProperties();
            foreach (var key in keyVal.Keys) {
                var prop = props.FirstOrDefault(x => x.Name == key);
                if (prop is null || !prop.CanWrite) continue;
                // var converter = TypeDescriptor.GetConverter(prop.PropertyType);
                // var parsedVal = converter.ConvertFromInvariantString(keyVal[key]?.ToString());
                prop.SetValue(instance, keyVal[key]);
            }
            return instance;
        }

        public static PatchVM MapToPatch<T>(this T com, string table = null)
        {
            if (com is null) return null;
            var type = typeof(T);
            var patch = new PatchVM
            {
                Table = table ?? type.Name,
            };
            var props = typeof(T).GetProperties().ToList();
            props.ForEach(prop =>
            {
                var val = prop.GetValue(com);
                patch.Changes.Add(new PatchDetail
                {
                    Field = prop.Name,
                    Value = val?.ToString()
                });
            });
            return patch;
        }
    }
}
