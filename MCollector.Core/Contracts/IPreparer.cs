using MCollector.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Core.Contracts
{
    public interface IPreparer : IAsSingleton //INamedAction //统一注入了Keyed<xx>
    {
        public string Name { get; }

        public Task Process(CollectTarget target, Dictionary<string, object> args);
    }

    //public abstract class PreparerBase : PreparerBase<Dictionary<string, object>>
    //{
        
    //}

    public abstract class PreparerBase<T> : IPreparer where T : class, new()
    {
        public abstract string Name { get; }

        public Task Process(CollectTarget target, Dictionary<string, object> args)
        {
            return Process(target, SerializerHelper.CreateFrom<T>(args));
        }

        protected abstract Task Process(CollectTarget target, T args);
    }
}
