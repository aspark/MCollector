using MCollector.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Core.Transformers
{
    internal class CountTransformer : TransformerBase<SearchTransformerArgs>
    {
        public override string Name => "count";

        public override Task<IEnumerable<CollectedData>> Run(CollectTarget target, IEnumerable<CollectedData> items, Dictionary<string, object> args)
        {
            if (items.Any() == false)
            {
                //throw new Exception("没有任何内容");
                Task.FromResult(new List<CollectedData>() { new FinalCollectedData(target.Name, target) { Content = "0" } });
            }

            var old = items.First();

            var data = new CollectedData(old.Name, old.Target);
            data.Content = items.Count().ToString();

            return Task.FromResult((IEnumerable<CollectedData>)new[] { data });
        }

        public override Task<TransformResult> Transform(CollectedData rawData, SearchTransformerArgs args)
        {
            return Task.FromResult(TransformResult.CreateFailed());//忽略，已经在Run中重载
        }
    }
}
