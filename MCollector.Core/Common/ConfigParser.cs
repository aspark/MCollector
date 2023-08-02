using MCollector.Core.Common;
using MCollector.Core.Config;

namespace MCollector.Core
{
    public class ConfigParser
    {
        private static Lazy<CollectorConfig> _config = new Lazy<CollectorConfig>(() => Parse(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "collector.yml")));

        private static CollectorConfig Parse(string file)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(file);
            }
            var config = SerializerHelper.Deserialize<CollectorConfig>(File.ReadAllText(file));

            return config;
        }

        public static CollectorConfig GetConfig()
        {
            return _config.Value;
        }
    }
}
