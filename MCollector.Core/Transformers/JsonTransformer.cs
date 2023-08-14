using MCollector.Core.Common;
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

            //改为不区分大小写
            if (args.ContentMapper != null)
                args.ContentMapper = new Dictionary<string, string>(args.ContentMapper, StringComparer.InvariantCultureIgnoreCase);

            if (string.IsNullOrEmpty(rawData.Content) == false)
            {
                var json = JsonSerializer.Deserialize<JsonElement>(rawData.Content);

                var root = json;
                if (!string.IsNullOrWhiteSpace(args.RootPath))
                {
                    root = SerializerHelper.GetElement(root, args.RootPath);
                }

                if (root.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement jsonItem in root.EnumerateArray())
                    {
                        AppendItem(rawData, args, jsonItem, ref list);
                    }
                }
                else if (root.ValueKind == JsonValueKind.Object)
                {
                    AppendItem(rawData, args, root, ref list);
                }

                results = list.AsEnumerable();

                if (list.Any())
                {
                    if(args.ReserveRawData)
                        list.Insert(0, rawData);

                    return true;
                }
            }

            return false;
        }

        private bool AppendItem(CollectedData rawData, JsonTransformerArgs args, JsonElement element, ref List<CollectedData> items)
        {
            //var data = new CollectedData(rawData.Name, rawData.Target);
            //data.Duration = rawData.Duration;
            //data.IsSuccess = rawData.IsSuccess;
            //data.Headers = rawData.Headers;

            if (args.ExtractNameFromProperty)
            {
                foreach (var prop in element.EnumerateObject())
                {
                    CollectedData data = new CollectedData(rawData.Name, rawData.Target).CopyFrom(rawData);
                    data.IsSuccess = true;//属于新转换的内容了，所以重新附值
                    data.LastCollectTime = DateTime.Now;
                    data.Name += (">" + prop.Name);
                    if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        data.Content = MapContent(prop.Value, args.ExtractContentFrom, args.ContentMapper);
                    }
                    else
                    {
                        data.Content = MapContent(prop.Value, args.ContentMapper);
                    }

                    items.Add(data);
                }

                return true;
            }
            else
            {
                if (element.TryGetProperty(args.ExtractNameFrom, out JsonElement elName))
                {
                    CollectedData data = new CollectedData(rawData.Name, rawData.Target).CopyFrom(rawData);
                    data.Name += (">" + elName.GetString());
                    data.IsSuccess = true;
                    data.LastCollectTime = DateTime.Now;
                    if (element.TryGetProperty(args.ExtractContentFrom, out JsonElement elContent))
                    {
                        data.Content = MapContent(elContent, args.ContentMapper);

                        items.Add(data);

                        return true;
                    }
                }
            }

            return false;
        }

        private string MapContent(JsonElement element, Dictionary<string, string> mapper)
        {
            var content = element.GetString() ?? "";

            if (!string.IsNullOrEmpty(content) && mapper?.ContainsKey(content) == true)
            {
                return mapper[content];
            }

            return content;
        }

        private string MapContent(JsonElement element, string path, Dictionary<string, string> mapper)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                element = SerializerHelper.GetElement(element, path);
            }

            return MapContent(element, mapper);
        }
    }

    internal class JsonTransformerArgs
    {
        public string RootPath { get; set; } = string.Empty;

        public string ExtractNameFrom { get; set; } = "name";

        /// <summary>
        /// 将对象所有属性处理为kv
        /// </summary>
        public bool ExtractNameFromProperty { get; set; } = false;

        public string ExtractContentFrom { get; set; } = "content";

        public Dictionary<string, string> ContentMapper { get; set; }

        /// <summary>
        /// 是否保留原target data
        /// </summary>
        public bool ReserveRawData { get; set; } = true;
    }

    //public enum JsonMapType
    //{
    //    Object,
    //    Array
    //}
}
