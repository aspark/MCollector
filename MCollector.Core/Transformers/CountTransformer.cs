using MCollector.Core.Common;
using MCollector.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MCollector.Core.Transformers
{
    internal class CountTransformer : ITransformer//TransformerBase<CountTransformerArgs>
    {
        public string Name => "count";

        public Task<IEnumerable<CollectedData>> Run(CollectTarget target, IEnumerable<CollectedData> items, Dictionary<string, object> args)
        {
            if (items.Any() == false)
            {
                return Task.FromResult((IEnumerable<CollectedData>)new [] { new CollectedData(target.Name, target) { Content = "0" } });
            }

            var typedArgs = SerializerHelper.CreateFrom<CountTransformerArgs>(args) ?? new CountTransformerArgs();

            var counts = new List<long>();
            foreach(var item in items)
            {
                var count = 1L;
                if (typedArgs.AsJson)
                {
                    if (!string.IsNullOrEmpty(item.Content))
                    {
                        var json = JsonSerializer.Deserialize<JsonElement>(item.Content);
                        if (json.ValueKind == JsonValueKind.Array)
                        {
                            count = json.EnumerateArray().Count();
                        }
                    }
                }

                counts.Add(count);
            }

            IEnumerable<CollectedData> results = null;

            if(typedArgs.Mode == CountTransformerArgs_CountMode.Summary)
            {
                results = new[] { new CollectedData(target.Name, target) { Content = counts.Sum().ToString() } };
            }
            else
            {
                results = counts.Select(c => new CollectedData(target.Name, target) { Content = c.ToString() });
            }

            return Task.FromResult(results);
        }
    }

    public class CountTransformerArgs
    {
        /// <summary>
        /// 统计模式，
        /// </summary>
        public CountTransformerArgs_CountMode Mode { get; set; }

        /// <summary>
        /// 是否解析为json后统计数组数量
        /// </summary>
        public bool AsJson { get; set; } = false;
    }

    public enum CountTransformerArgs_CountMode
    {
        Summary = 0,

        Keep,
    }
}
