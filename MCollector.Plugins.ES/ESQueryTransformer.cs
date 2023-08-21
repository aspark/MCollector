

using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using MCollector.Core.Common;
using MCollector.Core.Contracts;
using MCollector.Core.Transformers;
using System.Text.Json;

namespace MCollector.Plugins.ES
{
    /// <summary>
    /// 将输入作为ES语句执行
    /// </summary>
    internal class ESQueryTransformer : TransformerBase<ESQueryTransformerArgs>
    {
        public override string Name => "es.q";

        public override async Task<TransformResult> Transform(CollectedData rawData, ESQueryTransformerArgs args)
        {
            args = args ?? new ESQueryTransformerArgs();

            var items = new List<CollectedData>();

            if (!string.IsNullOrWhiteSpace(rawData.Content))
            {
                var data = new CollectedData(rawData.Target.Name, rawData.Target);

                data.Content = await ESQueryHelper.Query(args.Server, args.Username, args.Password, rawData.Content, args.QueryTarget, args.Parameters);

                items.Add(data);

                return TransformResult.CreateSuccess(items);
            }

            return TransformResult.CreateFailed();
        }
    }

    internal class ESQueryTransformerArgs : ESQueryArgs
    {
        public string Server { get; set; } = "https://localhost:9200";
    }
}