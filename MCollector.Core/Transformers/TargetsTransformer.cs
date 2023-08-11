using MCollector.Core.Common;
using MCollector.Core.Config;
using MCollector.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MCollector.Core.Transformers
{
    internal class TargetsTransformer : TransformerBase<TargetsTransformerArgs>, ITransformer
    {
        ICollectTargetManager _targetManager = null;

        public override string Name => "targets";

        public TargetsTransformer(ICollectTargetManager targetManager)//todo inject config
        {
            _targetManager = targetManager;
        }

        public override bool Transform(CollectedData rawData, TargetsTransformerArgs args, out IEnumerable<CollectedData> results)
        {
            var items = new List<CollectedData>();
            results = items;
            if (rawData.IsSuccess && string.IsNullOrWhiteSpace(rawData.Content) == false)
            {
                var data = new CollectedData(rawData.Name, rawData.Target);

                var targets = ConvertToTargets(rawData.Content, args);
                if (targets?.Any() == true)
                {
                    targets = targets.Where(t => !string.Equals(t.Type, "cmd", StringComparison.InvariantCultureIgnoreCase)).ToList();//暂不从远程加载cmd类型的target
                    targets.ForEach(t => t.Trace = rawData.Target.GetVersion());
                    _targetManager.Merge(targets);
                }

                data.Content = $"Add {targets?.Count()} Targets";

                items.Add(data);

                return true;
            }

            return false;
        }

        private List<CollectTarget> ConvertToTargets(string content, TargetsTransformerArgs args)
        {
            if (!string.IsNullOrWhiteSpace(content))
            {
                if(content.StartsWith("{") || content.StartsWith("["))//json格式
                {
                    var json = JsonSerializer.Deserialize<JsonElement>(content);

                    var root = json;
                    if (!string.IsNullOrWhiteSpace(args.RootPath))
                    {
                        root = SerializerHelper.GetElement(root, args.RootPath);
                    }

                    var targets = root.Deserialize<List<CollectTarget>>();
                }
                else//以yml格式
                {
                    var yml= SerializerHelper.Deserialize<CollectorConfig>(content);//暂时以Config类来反序列化

                    return yml.Targets?.ToList() ?? new List<CollectTarget>();
                }
            }

            return new List<CollectTarget>();
        }
    }
    internal class TargetsTransformerArgs
    {
        public string RootPath { get; set; } = string.Empty;
    }

}
