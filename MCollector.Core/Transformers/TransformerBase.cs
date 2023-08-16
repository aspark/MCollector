using MCollector.Core.Common;
using MCollector.Core.Contracts;

namespace MCollector.Core.Transformers
{
    public abstract class TransformerBase<T> : ITransformer where T : class, new()
    {
        public abstract string Name { get; }

        public virtual Task<IEnumerable<CollectedData>> Run(IEnumerable<CollectedData> items, Dictionary<string, object> args)
        {
            var typedArgs = SerializerHelper.CreateFrom<T>(args);

            var results = new List<CollectedData>();
            foreach (var item in items)
            {
                if (Transform(item, typedArgs, out IEnumerable<CollectedData> transformedItems))
                {
                    results.AddRange(transformedItems);
                }
                else
                {//转换失败，原样返回
                    results.Add(item);
                }
            }

            return Task.FromResult(results.AsEnumerable());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="args"></param>
        /// <param name="results"></param>
        /// <returns>返回是否转换成功，如果失败则会将传入的item原样返回</returns>
        public abstract bool Transform(CollectedData rawData, T args, out IEnumerable<CollectedData> results);

    }

    public abstract class TransformerBase : TransformerBase<Dictionary<string, object>>
    {

    }
}
