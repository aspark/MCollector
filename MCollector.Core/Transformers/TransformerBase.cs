using MCollector.Core.Common;
using MCollector.Core.Contracts;

namespace MCollector.Core.Transformers
{
    public abstract class TransformerBase<T> : ITransformer where T : class, new()
    {
        public abstract string Name { get; }

        public virtual async Task<IEnumerable<CollectedData>> Run(CollectTarget target, IEnumerable<CollectedData> items, Dictionary<string, object> args)
        {
            var typedArgs = SerializerHelper.CreateFrom<T>(args);

            var results = new List<CollectedData>();
            foreach (var item in items)
            {
                var result = await Transform(item, typedArgs);
                if (result?.IsSuccess == true)
                {
                    results.AddRange(result.Items);
                }
                else
                {//转换失败，原样返回
                    results.Add(item);
                }
            }

            return results.AsEnumerable();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rawData"></param>
        /// <param name="args"></param>
        /// <param name="results"></param>
        /// <returns>返回是否转换成功，如果失败则会将传入的item原样返回</returns>
        public abstract Task<TransformResult> Transform(CollectedData rawData, T args);

    }

    public abstract class TransformerBase : TransformerBase<Dictionary<string, object>>
    {

    }

    public class TransformResult
    {
        public TransformResult()
        {
            
        }

        public TransformResult(bool isSuccess, IEnumerable<CollectedData> items)
        {
            IsSuccess = isSuccess;
            Items = items;
        }

        public bool IsSuccess { get; set; } = false;

        public IEnumerable<CollectedData> Items { get; set; }

        public static TransformResult CreateFailed()
        {
            return new TransformResult(false, new CollectedData[0]);
        }

        public static TransformResult CreateSuccess(IEnumerable<CollectedData> items)
        {
            return new TransformResult(true, items);
        }
    }
}
