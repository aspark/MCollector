using MCollector.Core.Contracts;
using MongoDB.Driver.Core.Configuration;
using MongoDB.Driver;
using System.Collections;
using System.Text.Json;
using MongoDB.Bson;

namespace MCollector.Plugins.MongoDB
{
    public class MongoDBCollector : CollectorBase<MongoDBArgs>
    {
        public override string Type => "mongodb";

        public override async Task<CollectedData> Collect(CollectTarget target, MongoDBArgs args)
        {
            var data = new CollectedData(target.Name, target);

            data.Content = await MongoDBHelper.Query(target.Target, target.Contents, args);

            return data;
        }
    }

    public class MongoDBArgs
    {
        public string db { get; set; }

        public string Collection { get; set; }

        public int DefaultLimit { get; set; } = 30;

        public MongoDBArgs_OutputMode Output { get; set; }
    }

    public enum MongoDBArgs_OutputMode
    {
        Details = 0,

        TotalCount
    }
}