using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Reflection;
using System.Xml.Linq;
using System.ComponentModel;
using System.Text.Json;
using YamlDotNet.Core.Tokens;

namespace MCollector.Core.Common
{
    public class SerializerHelper
    {
        public static T Deserialize<T>(Dictionary<string, object> values) where T : class, new()
        {
            if(values?.Count > 0)
            {
                var convertValues = values.ToDictionary(p => p.Key, p => {
                    if (p.Value?.GetType().IsAssignableTo(typeof(JsonElement)) == true)//jsonElement需要toString避免多层属性
                        return p.Value.ToString();

                    return p.Value;
                });

                return Deserialize<T>(new Serializer().Serialize(convertValues));//没有使用内置的json的原因是：无法将string转为int
            }

            return default;
        }

        public static string ToPlainString(object obj)
        {
            return new Serializer().Serialize(obj)?.Replace("\r", "")?.Replace("\n", "")?.Replace(" ", "") ?? "";
        }

        public static T Deserialize<T>(string content) where T : class, new()
        {
            DeserializerBuilder builder = new DeserializerBuilder();
            builder.IgnoreUnmatchedProperties();
            builder.WithNamingConvention(CamelCaseNamingConvention.Instance);

            var des = builder.Build();

            return des.Deserialize<T>(content);
        }

        /// <summary>
        /// 只绑定值类型的属性
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static T AssignFrom<T>(T obj, Dictionary<string, object> values)
        {
            var type = typeof(T);
            foreach (var kv in values)
            {
                var prop = type.GetProperty(kv.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && kv.Value!=null)
                {
                    var val = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromString(kv.Value.ToString()!);

                    prop.SetValue(obj, val, null);
                }
            }

            return obj;
        }

        public static T? CreateFrom<T>(Dictionary<string, object> values) where T : class, new()
        {
            values ??= new Dictionary<string, object>();
            if (typeof(Dictionary<string, object>) == typeof(T))
            {
                return (T)(object)values;
            }

            return SerializerHelper.Deserialize<T>(values);
        }

        /// <summary>
        /// 失败时result==element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="path"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool TryGetElement(JsonElement element, string path, out JsonElement result)
        {
            result = element;
            try
            {
                result = GetElement(element, path);
                return true;
            }
            catch { }

            return false;
        }

        public static JsonElement GetElement(JsonElement element, string path)
        {
            var target = element;
            if (!string.IsNullOrWhiteSpace(path))
            {
                foreach (var seg in path.Split('.'))
                {
                    if (seg.EndsWith(']'))
                    {
                        var index = seg.Split('[', ']');
                        target = target.GetProperty(index[0]);
                        if (target.ValueKind == JsonValueKind.Array)
                        {
                            target = target[int.Parse(index[0])];
                        }
                    }
                    else
                    {
                        target = target.GetProperty(seg);
                    }
                }
            }

            return target;
        }
    }
}
