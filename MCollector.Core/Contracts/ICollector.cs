using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Core.Contracts
{
    public interface ICollector
    {
        public string Type { get; }

        //public int Priority { get; }

        public Task<CollectedData> Collect(CollectTarget target);
    }

    //public abstract class CollectorBase
    //{
    //    public CollectorBase()
    //    {

    //    }
    //}
}
