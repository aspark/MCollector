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

        Task<IEnumerable<CollectedData>> Run(IEnumerable<CollectedData> items, Dictionary<string, object> args);
    }
}
