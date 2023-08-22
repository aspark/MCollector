using MCollector.Core.Contracts;
using MCollector.Core.Transformers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Plugins.MongoDB
{
    internal class MongoDBTransformer : TransformerBase<MongoDBTransformerArgs>
    {
        public override string Name => "mongodb";

        public override async Task<TransformResult> Transform(CollectedData rawData, MongoDBTransformerArgs args)
        {
            if (!string.IsNullOrWhiteSpace(rawData.Content))
            {
                var data = new CollectedData(rawData.Name, rawData.Target);
                data.Content = await MongoDBHelper.Query(args.Server, new[] { rawData.Content }, args);

                return TransformResult.CreateSuccess(new[] { data });
            }

            return TransformResult.CreateFailed();
        }
    }

    public class MongoDBTransformerArgs : MongoDBArgs
    {
        public string Server { get; set; } = "http://localhost:27017";
    }
}
