using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCollector.Core.Contracts
{
    public interface ITransformer
    {
        public string Name { get; }

        Task<IEnumerable<CollectedData>> Run(CollectTarget target, IEnumerable<CollectedData> items, Dictionary<string, object> args);
    }

    public interface ITransformerRunner: IAsSingleton
    {
        Task<IEnumerable<CollectedData>> Transform(CollectTarget target, IEnumerable<CollectedData> items, Dictionary<string, Dictionary<string, object>> transformers);
    }

    internal class DefaultTransformerRunner : ITransformerRunner
    {
        Dictionary<string, ITransformer> _transforms = new Dictionary<string, ITransformer>(StringComparer.InvariantCultureIgnoreCase);

        public DefaultTransformerRunner(IList<ITransformer> transformers)
        {

            foreach (var tranformer in transformers)
            {
                _transforms[tranformer.Name] = tranformer;
            }
        }

        public async Task<IEnumerable<CollectedData>> Transform(CollectTarget target, IEnumerable<CollectedData> items, Dictionary<string, Dictionary<string, object>> transformers)
        {
            if (transformers?.Any() == true)
            {
                var changedItems = new List<CollectedData>();
                foreach (var item in items)
                {
                    foreach (var trans in transformers)
                    {
                        if (item is FinalCollectedData || (!item.IsSuccess && item.StopWhenFailed)) //不成功的继续transform，url可能部分失败
                        {
                            changedItems.Add(item);

                            break;
                        }

                        if (_transforms.ContainsKey(trans.Key))
                        {
                            changedItems.AddRange(await _transforms[trans.Key].Run(target, new[] { item }, trans.Value));
                        }
                    }
                }

                return changedItems;
            }

            return items;
        }

    }
}
