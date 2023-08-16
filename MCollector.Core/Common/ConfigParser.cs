using MCollector.Core.Common;
using MCollector.Core.Config;
using MCollector.Core.Contracts;

namespace MCollector.Core
{
    public interface IConfigParser : IAsSingleton
    {
        CollectorConfig Get();
    }

    internal class ConfigParser: IConfigParser
    {
        //private Lazy<CollectorConfig> _config = new Lazy<CollectorConfig>(() => Get());
        IProtector _protector;

        public ConfigParser(IProtector protector)
        {
            _protector = protector;
        }

        public CollectorConfig Get()
        {
            var file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "collector.yml");
            var config = Parse(file, true)!;

            foreach (var subFile in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "collector.*.yml").Order())
            {
                var subConfig = Parse(subFile, false);
                if (subConfig != null)
                {
                    //merge targets
                    config.Targets = config.Targets.Concat(subConfig.Targets).ToArray();

                    //merge exporter
                    if(subConfig.Exporter?.Any() == true)
                    {
                        config.Exporter ??= new CollectorExporterConfig();
                        foreach (var exporter in subConfig.Exporter)
                        {
                            config.Exporter[exporter.Key] = exporter.Value;
                        }
                    }
                }
            }

            return config;
        }

        private CollectorConfig? Parse(string file, bool throwException = true)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException(file);
            }

            try
            {
                var content = File.ReadAllText(file);
                //解密
                content = _protector.FindAndUnprotectText(content);

                var config = SerializerHelper.Deserialize<CollectorConfig>(content);

                return config;
            }
            catch (Exception ex)
            {
                var msg = $"从配置文件加载失败：{Path.GetFileName(file)}, {ex.Message}";
                if (throwException)
                    throw new Exception(msg, ex);
                else
                    Console.WriteLine(msg);
            }

            return default;
        }

        //public CollectorConfig GetConfig()
        //{
        //    return _config.Value;
        //}
    }
}
