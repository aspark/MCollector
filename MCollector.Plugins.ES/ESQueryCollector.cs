

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Clients.Elasticsearch.Requests;
using Elastic.Transport;
using MCollector.Core.Common;
using MCollector.Core.Contracts;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using YamlDotNet.Serialization;

namespace MCollector.Plugins.ES
{
    public class ESQueryCollector : ICollector
    {
        public string Type => "es.q";

        public async Task<CollectedData> Collect(CollectTarget target)
        {
            var data = new CollectedData(target.Name, target);

            var args = SerializerHelper.CreateFrom<ESQueryCollectorArgs>(target.Args) ?? new ESQueryCollectorArgs();


            if(target.Contents?.Any() == true)
            {
                //var result = await client.SearchAsync<dynamic>(s=>s.From(0).Size(1).Query(q=>q.QueryString(""));
                //foreach (var content in target.Contents)
                //{
                //}

                //暂以最后一个content为query，
                var query = target.Contents.Last();




                //data.Content = result.Total.ToString();
                data.Content = await ESQueryHelper.Query(target.Target, args.Username, args.Password, query, args.QueryTarget, args.Parameters);
            }

            return data;
        }
    }

    internal class ESQueryCollectorArgs : ESQueryArgs
    {

    }
}