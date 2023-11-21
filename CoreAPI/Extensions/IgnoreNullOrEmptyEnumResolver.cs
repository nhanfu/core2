using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Collections;
using System.Reflection;

namespace Core.Extensions
{
    public class DateParser : IsoDateTimeConverter
    {
        public override object ReadJson(JsonReader reader, Type type, object existingValue, JsonSerializer serializer)
        {
            var isNull = type == typeof(DateTimeOffset?);
            if (type == typeof(DateTimeOffset) || isNull)
            {
                string dateText = reader.Value?.ToString();
                if (DateTimeOffset.TryParse(dateText, out var res))
                    return res;
                else return isNull ? null : DateTimeOffset.MinValue;
            }
            return base.ReadJson(reader, type, existingValue, serializer);
        }
    }
    
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
                        else if (value is IEnumerable enumerable)
                        {
                            return enumerable.GetEnumerator().MoveNext();
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
