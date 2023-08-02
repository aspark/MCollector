using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization;
using System.Reflection;
using System.Xml.Linq;
using System.ComponentModel;

namespace MCollector.Core.Common
{
    public class SerializerHelper
    {
        public static T Deserialize<T>(Dictionary<string, object> values) where T : class, new()
        {
            return Deserialize<T>(new Serializer().Serialize(values));
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
    }
}
