using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Core.Extensions
{
    public class IgnoreNullOrEmptyEnumResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            property.ShouldSerialize = instance =>
            {
                object value = null;
                switch (member.MemberType)
                {
                    case MemberTypes.Property:
                        var prop = instance.GetType().GetProperty(member.Name);
                        if (!prop.CanRead || !prop.CanWrite || !prop.PropertyType.IsPublic)
                        {
                            return false;
                        }
                        value = prop.GetValue(instance);
                        if (value is null)
                        {
                            return true;
                        }

                        if (value is IQueryable queryable)
                        {
                            return true;
                        }
                        else if (value is IEnumerable enumerable)
                        {
                            if (!enumerable.GetEnumerator().MoveNext())
                            {
                                return false;
                            }
                        }
                        return true;
                    case MemberTypes.Field:
                        return false;
                    default:
                        return false;

                }
            };
            return property;
        }
    }
}
