using MCollector.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Core.Transformers
{
    internal class SearchTransformer : TransformerBase<SearchTransformerArgs>
    {
        public override string Name => "search";

        public override bool Transform(CollectedData rawData, SearchTransformerArgs args, out IEnumerable<CollectedData> results)
        {
            var data = new FinalCollectedData(rawData.Name, rawData.Target).CopyFrom(rawData);
            data.IsSuccess = false;

            results = new List<CollectedData>() { data };

            if (string.IsNullOrEmpty(args.Text) || rawData.Content?.IndexOf(args.Text, StringComparison.InvariantCultureIgnoreCase) >= 0)
            {
                data.IsSuccess = true;
            }
            else
            {
                data.Content = $"无法查找到：{args.Text}，Content length：{rawData.Content?.Length ?? 0}";
            }

            return true;
        }
    }

    internal class SearchTransformerArgs
    {
        public string Text { get; set; }
    }
}
