using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MCollector.Plugins.MongoDB
{
    internal class MongoDBHelper
    {
        public static async Task<string> Query(string server, string[]? contents, MongoDBArgs args)
        {
            var client = new MongoClient(server);

            var db = client.GetDatabase(args.db);

            if (contents?.Any() == true)
            {
                var collection = db.GetCollection<dynamic>(args.Collection);

                //BsonDocument.Parse(query)
                //var result = await db.RunCommandAsync<dynamic>(query);

                if (contents.Length == 1)
                {
                    var query = contents.Last();//$filter
                    var find = collection.Find(query);
                    if (args.DefaultLimit > 0)
                    {
                        find = find.Limit(args.DefaultLimit);
                    }

                    if (args.Output == MongoDBArgs_OutputMode.TotalCount)
                    {
                        return (await find.CountDocumentsAsync()).ToString();
                    }
                    else
                    {
                        return JsonSerializer.Serialize(await find.ToListAsync());
                    }
                }
                else
                {
                    PipelineDefinition<dynamic, dynamic> pipeline = contents.Select(c => BsonDocument.Parse(c)).ToList();
                    var aggr = await collection.AggregateAsync<dynamic>(pipeline);
                    var result = await aggr.ToListAsync();

                    if (args.Output == MongoDBArgs_OutputMode.TotalCount)
                    {
                        return result.Count.ToString();
                    }
                    else
                    {
                        if (args.DefaultLimit > 0)
                            return JsonSerializer.Serialize(result.Take(args.DefaultLimit));
                        else
                            return JsonSerializer.Serialize(result);
                    }
                }
            }

            return string.Empty;
        }
    }
}
