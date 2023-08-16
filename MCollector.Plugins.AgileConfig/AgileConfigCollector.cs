using AgileConfig.Client;
using MCollector.Core.Common;
using MCollector.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace MCollector.Plugins.AgileConfig
{
    public class AgileConfigCollector : ICollector, IDisposable
    {

        public string Type => "agileConfig";

        ConcurrentDictionary<string, IConfigClient> _clients = new ConcurrentDictionary<string, IConfigClient>();
        public async Task<CollectedData> Collect(CollectTarget target)
        {
            var opts = SerializerHelper.CreateFrom<AgileConfigCollectorArgs>(target.Args);
            opts.Nodes = target.Target;

            var data = new CollectedData(target.Name, target);

            var _configClient = _clients.GetOrAdd(target.Name, k => new ConfigClient(new ConfigClientOptions
            {
                AppId = opts.AppId,
                Secret = opts.Secret,
                Nodes = opts.Nodes,
                Name = opts.Name,
                Tag = opts.Tag,
                ENV = opts.Env,
                HttpTimeout = opts.HttpTimeout,
                CacheDirectory = opts.CacheDirectory,
                ConfigCacheEncrypt = opts.ConfigCacheEncrypt
            }));
            //var _configClient = new ConfigClient(opts);
            try
            {
                if(_configClient.Status != ConnectStatus.Connected)
                {
                    await _configClient.ConnectAsync();
                }

                if (_configClient.Status == ConnectStatus.Connected)
                {
                    var content = string.Empty;

                    //_configClient.
                    if (_configClient.Data.Any())
                    {
                        JToken token = Regex.IsMatch(_configClient.Data.First().Key, @"^\d") ? new JArray() : new JObject();//{[targets:0:name, from agile]} agile无法将数组配置到根

                        foreach (var pair in _configClient.Data)
                        {
                            Add(token, pair.Key, pair.Value);
                        }

                        content = JsonConvert.SerializeObject(token);
                        data.Content = content;
                    }

                    //await _configClient.DisconnectAsync();
                }
                else
                {
                    data.IsSuccess = false;
                    data.Content = "连接失败";
                }
            }
            catch (Exception ex)
            {
                data.IsSuccess = false;
                data.Content = ex.GetBaseException().Message;
            }

            return data;
        }

        private void Add(JToken jToken, string key, string value)
        {
            var components = key.Split(":", 3);
            if (components.Length == 1)
            {
                // Leaf node
                if (jToken is JArray jArray_)
                {
                    jArray_.Add(value);
                }
                else
                {
                    jToken[components[0]] = value;
                }
                return;
            }

            // Next level
            JToken nextToken;
            var nextTokenIsAnArray = int.TryParse(components[1], out _);
            if (jToken is JArray jArray)
            {
                var index = int.Parse(components[0]);
                if (jArray.Count == index)
                {
                    nextToken = nextTokenIsAnArray ? new JArray() : (JToken)new JObject();
                    jArray.Add(nextToken);
                }
                else
                {
                    nextToken = jArray[index];
                }
            }
            else
            {
                nextToken = jToken[components[0]];
                if (nextToken == null)
                {
                    nextToken = jToken[components[0]] = nextTokenIsAnArray ? new JArray() : (JToken)new JObject();
                }
            }

            Add(nextToken, key[(components[0].Length + 1)..], value);
        }


        public void Dispose()
        {
            foreach(var cli in _clients)
            {
                cli.Value.DisconnectAsync();
            }
        }
    }

    //使用ConfigClientOptions即可
    internal class AgileConfigCollectorArgs//: ConfigClientOptions env全大写，无法反序列化。。。
    {
        ////读取的路径
        //public string Path { get; set; } = "targets";

        public string AppId { get; set; }

        public string Secret { get; set; }

        public string Nodes { get; set; }

        public string Name { get; set; }

        public string Tag { get; set; }

        [YamlMember(Alias = "env")]
        public string Env { get; set; } = "PROD";

        public int HttpTimeout { get; set; } = 100;


        public string CacheDirectory { get; set; }

        public bool ConfigCacheEncrypt { get; set; }
    }
}