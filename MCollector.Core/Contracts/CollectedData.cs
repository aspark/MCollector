using System.Reflection;

namespace MCollector.Core.Contracts
{
    public class CollectedData
    {
        //public CollectedData(CollectedData from)
        //{
        //    this.Name = from.Name;
        //    this.Target = from.Target;
        //}

        public CollectedData(string name, CollectTarget target)
        {
            Name = name;
            Target = target;
        }

        //public CollectedData(CollectedData target) :this(target.Name, target.Target)
        //{
        //    IsSuccess = target.IsSuccess;
        //    Headers = new Dictionary<string, object>(target.Headers);
        //    Duration = target.Duration;
        //    LastUpdateTime = target.LastUpdateTime;
        //}

        //public Guid ID { get; set; } = Guid.NewGuid();

        public string Name { get; set; }

        public CollectTarget Target { get; private set; }

        //public HttpStatusCode Code { get; set; } = HttpStatusCode.OK;
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// 上下文
        /// </summary>
        public Dictionary<string, object> Headers { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 返回的内容
        /// </summary>
        public string? Content { get; set; }//object

        /// <summary>
        /// 耗时
        /// </summary>
        public long Duration { get; set; }

        public DateTime LastCollectTime { get; set; } = DateTime.Now;
    }

    public class FinalCollectedData : CollectedData
    {
        public FinalCollectedData(string name, CollectTarget target) : base(name, target)
        {

        }

        public bool IsFinal { get; private set; } = true;
    }

    public static class CollectedDataExtensions
    {

        public static IF CopyFrom<IF, TF>(this IF ori, TF from) where IF : CollectedData where TF : CollectedData
        {
            ori.IsSuccess = from.IsSuccess;
            ori.Headers = new Dictionary<string, object>(from.Headers);
            ori.Duration = from.Duration;
            ori.LastCollectTime = from.LastCollectTime;

            return ori;
        }
    }
}
