using MCollector.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Core.Contracts
{
    public interface ICollector: IAsSingleton
    {
        public string Type { get; }

        //public int Priority { get; }

        public Task<CollectedData> Collect(CollectTarget target);
    }

    public abstract class CollectorBase<T>: ICollector where T : class, new()
    {
        public CollectorBase()
        {

        }

        public abstract string Type { get; }

        public virtual Task<CollectedData> Collect(CollectTarget target)
        {
            return Collect(target, SerializerHelper.CreateFrom<T>(target.Args) ?? new T());
        }

        public abstract Task<CollectedData> Collect(CollectTarget target, T args);
    }
}
