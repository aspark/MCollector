using MCollector.Core.Common;
using MCollector.Core.Contracts;

namespace MCollector.Core.Transformers
{
    public abstract class TransformerBase<T> : ITransformer where T : class, new()
    {
        public abstract string Name { get; }

        public virtual Task<IEnumerable<CollectedData>> Run(IEnumerable<CollectedData> items, Dictionary<string, object> args)
        {
            var typedArgs = ConvertArgs(args);

            var results = new List<CollectedData>();
            foreach (var item in items)
            {
                if (Transform(item, typedArgs, out IEnumerable<CollectedData> transformedItems))
                {
                    results.AddRange(transformedItems);
                }
                else
                {
                    results.Add(item);
                }
            }

            return Task.FromResult(results.AsEnumerable());
        }

        public abstract bool Transform(CollectedData rawData, T args, out IEnumerable<CollectedData> results);

        private T? ConvertArgs(Dictionary<string, object> args)
        {
            args ??= new Dictionary<string, object>();
            if (typeof(Dictionary<string, object>) == typeof(T))
            {
                return (T)(object)args;
            }

            return SerializerHelper.Deserialize<T>(args);
        }
    }

    public abstract class TransformerBase : TransformerBase<Dictionary<string, object>>
    {

    }
}
