using System.ComponentModel;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Core.Tokens;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace MCollector.Core.Common
{
    public class SerializerHelper
    {
        private static Dictionary<string, object> ConvertJsonElement(Dictionary<string, object> values)
        {
            var convertValues = values.ToDictionary(p => p.Key, p =>
            {
                if ((p.Value is JsonElement element))//jsonElement需要toString避免多层属性，多层dic如何解决？
                {
                    return ConvertJsonElement(element);
                }

                return p.Value;
            });

            return convertValues;
        }

        private static object ConvertJsonElement(JsonElement element)
        {
            if(element.ValueKind == JsonValueKind.Object)
            {
                var dic = new Dictionary<string, object>(); 
                foreach(var obj in element.EnumerateObject())
                {
                    dic[obj.Name] = ConvertJsonElement(obj.Value);
                }

                return dic;
            }
            else if(element.ValueKind == JsonValueKind.Array)
            {
                var arr = new List<object>();
                foreach (var item in element.EnumerateArray())
                {
                    arr.Add(ConvertJsonElement(item));
                }

                return arr;
            }

            return element.ToString();
        }

        private static T? DeserializeFromObject<T>(object obj) where T : class, new()
        {
            if(obj is Dictionary<string, object> values)
            {
                return DeserializeFromDic<T>(values);
            }

            var builder = new SerializerBuilder();//.WithTypeConverter(new JsonElementConvert());

            var content = builder.Build().Serialize(obj is JsonElement elem ? ConvertJsonElement(elem) : obj);

            return Deserialize<T>(content);//没有使用内置的json的原因是：将string转为int时会报错，后期考虑收入newtonsoft~,yml对Json反序列化的object不太友好
        }

        private static T? DeserializeFromDic<T>(Dictionary<string, object> values) where T : class, new()
        {
            if(values?.Count > 0)
            {
                var convertedValues = ConvertJsonElement(values);

                var builder = new SerializerBuilder();//.WithTypeConverter(new JsonElementConvert());

                var content = builder.Build().Serialize(convertedValues);

                return Deserialize<T>(content);//没有使用内置的json的原因是：将string转为int时会报错，后期考虑收入newtonsoft~,yml对Json反序列化的object不太友好
            }

            return default;
        }

        /// <summary>
        /// 将yaml或json返序列化为对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="content"></param>
        /// <returns></returns>
        public static T? Deserialize<T>(string content) where T : class, new()
        {
            if (!string.IsNullOrEmpty(content))
            {
                if(content.StartsWith('{') || content.StartsWith('['))//json
                {
                    return JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions() { PropertyNameCaseInsensitive = false, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) ?? new();
                }
                else
                {
                    DeserializerBuilder builder = new DeserializerBuilder();
                    builder.IgnoreUnmatchedProperties();
                    builder.WithNamingConvention(CamelCaseNamingConvention.Instance);

                    var des = builder.Build();

                    return des.Deserialize<T>(content);
                }
            }

            return default;
        }

        /// <summary>
        /// 从字典创建对象，不会返回null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <returns></returns>
        public static T CreateFrom<T>(Dictionary<string, object> values) where T : class, new()
        {
            values ??= new Dictionary<string, object>();
            if (typeof(Dictionary<string, object>) == typeof(T))
            {
                return (T)(object)values;
            }

            return SerializerHelper.DeserializeFromDic<T>(values) ?? new T();
        }

        ///// <summary>
        ///// 只绑定值类型的属性
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="obj"></param>
        ///// <param name="values"></param>
        ///// <returns></returns>
        //public static T AssignFrom<T>(T obj, Dictionary<string, object> values)
        //{
        //    var type = typeof(T);
        //    //改为Deserilizer后同属性赋值？
        //    foreach (var kv in values)
        //    {
        //        var prop = type.GetProperty(kv.Key, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
        //        if (prop != null && kv.Value!=null)
        //        {
        //            var val = TypeDescriptor.GetConverter(prop.PropertyType).ConvertFromString(kv.Value.ToString()!);

        //            prop.SetValue(obj, val, null);
        //        }
        //    }

        //    return obj;
        //}

        public static bool TryGetObject(dynamic obj, string path, out dynamic result)
        {
            result = obj;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                foreach (var p in path.Trim().Split('.'))
                {
                    if (p.EndsWith(']'))
                    {
                        var index = ParseArrayPath(p);
                        result = result[index.propName];
                        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(result.GetType()))
                        {
                            var arrItem = GetElementAt((System.Collections.IEnumerable)result, index.index);

                            if (arrItem == null)
                                throw new IndexOutOfRangeException(p);

                            result = arrItem;
                        }
                    }
                    else
                    {
                        result = result[p];
                    }
                }

                return true;

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return false;
        }

        public static bool TryCreateFrom<T>(dynamic obj, string path, out T? result) where T: class, new ()
        {
            result = null;

            if (TryGetObject(obj, path, out dynamic target))
            {
                result = SerializerHelper.DeserializeFromObject<T>((object)target);

                return true;
            }

            return false;
        }

        public static string ToPlainString(object obj)
        {
            return new Serializer().Serialize(obj)?.Replace("\r", "")?.Replace("\n", "")?.Replace(" ", "") ?? "";
        }

        private static dynamic? GetElementAt(System.Collections.IEnumerable items, int index)
        {
            var i = 0;
            dynamic? finded = null;
            foreach (var item in items)
            {
                if (i == index)
                {
                    finded = item;
                    break;
                }

                i++;
            }

            return finded;
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
                        var index = ParseArrayPath(seg);

                        target = target.GetProperty(index.propName);
                        if (target.ValueKind == JsonValueKind.Array)
                        {
                            target = target[index.index];
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

        private static (string propName, int index) ParseArrayPath(string path)
        {
            var segs = path.Split('[', ']');
            var propName = segs[0];
            var index = int.Parse(segs[0]);

            return (propName, index);
        }
        //private class JsonElementConvert : IYamlTypeConverter
        //{
        //    public bool Accepts(Type type)
        //    {
        //        return type.IsAssignableTo(typeof(JsonElement));
        //    }

        //    public object? ReadYaml(IParser parser, Type type)
        //    {
        //        throw new NotImplementedException();
        //        //JsonSerializer.Serialize(parser.Consume<Scalar>().Value, type);
        //    }

        //    public void WriteYaml(IEmitter emitter, object? value, Type type)
        //    {
        //        var element = (JsonElement)value;

        //        if (element.ValueKind != JsonValueKind.Object && element.ValueKind != JsonValueKind.Array)
        //        {
        //            emitter.Emit(new Scalar(AnchorName.Empty, TagName.Empty, element.GetRawText().Trim('"').Replace("\"", "\\\""), ScalarStyle.Plain, true, false));
        //        }
        //        else
        //        {

        //        }
        //    }
        //}

    }
}
