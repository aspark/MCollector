using MCollector.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MCollector.Core.Transformers
{
    internal class JsonTransformer : TransformerBase<JsonTransformerArgs>, ITransformer
    {
        public override string Name => "json";


        //private const string _keyName = "extractNameFrom";
        //private const string _keyContent = "extractContentFrom";

        public override bool Transform(CollectedData rawData, JsonTransformerArgs args, out IEnumerable<CollectedData> results)
        {
            var list = new List<CollectedData>();
            results = list;

            if (string.IsNullOrEmpty(rawData.Content) == false)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(rawData.Content);
                if (json.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement jsonItem in json.EnumerateArray())
                    {
                        AppendItem(rawData, args, jsonItem, ref list);
                    }
                }
                else if (json.ValueKind == JsonValueKind.Object)
                {
                    AppendItem(rawData, args, json, ref list);
                }

                results = list.AsEnumerable();

                return list.Any();
            }

            return false;
        }

        private bool AppendItem(CollectedData rawData, JsonTransformerArgs args, JsonElement element, ref List<CollectedData> items)
        {
            //var data = new CollectedData(rawData.Name, rawData.Target);
            //data.Duration = rawData.Duration;
            //data.IsSuccess = rawData.IsSuccess;
            //data.Headers = rawData.Headers;
            CollectedData data;
            if (args.ExtractAllProperties)
            {
                foreach(var prop in element.EnumerateObject())
                {
                    data = new CollectedData(rawData.Name, rawData.Target).CopyFrom(rawData);
                    data.Name += (">" + prop.Name);
                    data.Content = prop.Value.GetString();
                    items.Add(data);
                }

                return true;
            }
            else
            {
                data = new CollectedData(rawData.Name, rawData.Target).CopyFrom(rawData);
                if (element.TryGetProperty(args.ExtractNameFrom, out JsonElement elName))
                {
                    data.Name += (">" + elName.GetString());
                    if (element.TryGetProperty(args.ExtractContentFrom, out JsonElement elContent))
                    {
                        data.Content = elContent.GetString();

                        items.Add(data);

                        return true;
                    }
                }
            }

            return false;
        }
    }

    internal class JsonTransformerArgs
    {
        public string ExtractNameFrom { get; set; } = "name";

        public string ExtractContentFrom { get; set; } = "content";

        /// <summary>
        /// 将对象所有属性处理为kv
        /// </summary>
        public bool ExtractAllProperties { get; set; } = false;
    }

    //public enum JsonMapType
    //{
    //    Object,
    //    Array
    //}
}
