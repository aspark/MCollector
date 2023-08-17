

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

            var args = SerializerHelper.CreateFrom<ESCollectorArgs>(target.Args) ?? new ESCollectorArgs();

            var settings = new ElasticsearchClientSettings(new Uri(target.Target));
            settings.Authentication(new BasicAuthentication(args.Username, args.Password)).ServerCertificateValidationCallback((obj, x, c, e) => true);

            var client = new ElasticsearchClient(settings);

            var stat = await client.Indices.StatsAsync();
            if(stat.IsValidResponse)
            {
                var dic = new Dictionary<string, HealthStatus>();

                if (stat.Indices != null)
                {
                    foreach (var pair in stat.Indices)
                    {
                        dic["indices-" + pair.Key] = pair.Value.Health ?? HealthStatus.Yellow;
                    }
                }

                dic["indices-" + args.IndicesSummaryName] = 
                    dic.Values.Any(v => v == HealthStatus.Red) 
                    ? HealthStatus.Red 
                    : (dic.Values.Any(v => v == HealthStatus.Yellow) ? HealthStatus.Yellow : HealthStatus.Green);

                data.Content = JsonSerializer.Serialize(dic);
            }
            else
            {
                data.IsSuccess = false;
                data.Content = stat.ElasticsearchServerError?.ToString();
            }

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