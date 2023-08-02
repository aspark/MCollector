using MCollector.Core.Common;
using System.Reflection;
using System.Text.Json;
using YamlDotNet.Serialization.NamingConventions;

namespace MCollector.Core.Config
{
    public class CollectorExporterConfig : Dictionary<string, Dictionary<string, object>>
    {
        public T GetConfig<T>(string name) where T : class, new()
        {
            if (this.ContainsKey(name))
            {
                //var config = Activator.CreateInstance(typeof(T)) as T;
                //var config = JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(this[name]), new JsonSerializerOptions { PropertyNameCaseInsensitive = true});
                var config = SerializerHelper.Deserialize<T>(this[name]);

                return config;
            }

            return default(T);
        }
    }

}
