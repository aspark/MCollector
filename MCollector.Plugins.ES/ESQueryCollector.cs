

using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using MCollector.Core.Common;
using MCollector.Core.Contracts;
using System.Text.Json;

namespace MCollector.Plugins.ES
{
    public class ESQueryCollector : ICollector
    {
        public string Type => "es.q";

        public async Task<CollectedData> Collect(CollectTarget target)
        {
            var data = new CollectedData(target.Name, target);

            //stat.All

            return data;
        }
    }

    internal class ESQueryCollectorArgs
    {
        public string Username { get; set; }

        public string Password { get; set; }

        //public string IndicesSummaryName { get; set; } = ".mcollect.summary";
    }
}