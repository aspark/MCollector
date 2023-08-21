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

            var client = new MongoClient(target.Target);

            var db = client.GetDatabase(args.db);

            if(target.Contents?.Any() == true)
            {
                var collection = db.GetCollection<dynamic>(args.Collection);
                if(target.Contents.Length == 1)
                {
                    var query = target.Contents.Last();//$filter
                    var result = collection.Find(query).Limit(args.DefaultLimit).ToList();
                    //collection.find("").Skip().Limit();
                    data.Content = JsonSerializer.Serialize(result);
                }
                else
                {
                    PipelineDefinition<dynamic, dynamic> stages = target.Contents.Select(c => BsonDocument.Parse(c)).ToList();
                    var result = await collection.AggregateAsync<dynamic>(stages);
                    data.Content = JsonSerializer.Serialize(result);
                }
            }

            //BsonDocument.Parse(query)
            //var result = await db.RunCommandAsync<dynamic>(query);

            return data;
        }
    }

    public class MongoDBArgs
    {
        public string db { get; set; }

        public string Collection { get; set; }

        public int DefaultLimit { get; set; } = 100;
    }
}