using MCollector.Core.Contracts;
using MCollector.Core.Transformers;

namespace MCollector.Test
{
    public class JsonTransformTest
    {
        [Fact]
        public void Test1()
        {
            var transform = new JsonTransformer();

            var target = new CollectTarget() { 
                Name = "test"
            };

            var data = new CollectedData(target.Name, target) { 
                Content = "{\r\n    \"status\": \"Healthy\",\r\n    \"results\": {\r\n        \"DB1\": {\r\n            \"status\": \"Degraded\",\r\n            \"description\": null,\r\n            \"data\": {}\r\n        },\r\n        \"DB1\": {\r\n            \"status\": \"Healthy\",\r\n            \"description\": null,\r\n            \"data\": {}\r\n        },\r\n        \"DB2\": {\r\n            \"status\": \"Unhealthy\",\r\n            \"description\": null,\r\n            \"data\": {}\r\n        }\r\n    }\r\n}"
            };

            //var items = new[] { target };
            var args = new JsonTransformerArgs()
            {
                RootPath = "results",
                ExtractNameFromProperty = true,
                ExtractContentFrom = "status",
                ContentMapper = new Dictionary<string, string>()
                {
                    {"Degraded", "0" },
                    {"Healthy", "1" },
                    {"Unhealthy", "0" },
                }
            };
            var result = transform.Transform(data, args, out var items);

            result.ShouldBe(true);
            items.Count().ShouldBe(4);
            items.All(i => i.IsSuccess).ShouldBe(true);
            items.Where(i => i.Content == "1").Count().ShouldBe(1);
            items.Where(i => i.Content == "0").Count().ShouldBe(2);

            
        }


        [Fact]
        public void Test2()
        {
            var transform = new JsonTransformer();

            var target = new CollectTarget()
            {
                Name = "test"
            };

            var data = new CollectedData(target.Name, target)
            {
                Content = "[\r\n    {\r\n        \"Name\": \"D1\",\r\n        \"Value\": 4\r\n    },\r\n    {\r\n        \"Name\": \"D2\",\r\n        \"Value\": 0\r\n    },\r\n    {\r\n        \"Name\": \"D3\",\r\n        \"Value\": 1\r\n    },\r\n    {\r\n        \"Name\": \"D4\",\r\n        \"Value\": 0.9999960294117127674705882353\r\n    },\r\n    {\r\n        \"Name\": \"D5\",\r\n        \"Value\": 0.9999999999999549975308641975\r\n    },\r\n    {\r\n        \"Name\": \"D6\",\r\n        \"Value\": 1.0000000000000025066892682927\r\n    },\r\n    {\r\n        \"Name\": \"D7\",\r\n        \"Value\": 1.0000000000\r\n    }\r\n]"
            };

            //var items = new[] { target };
            var args = new JsonTransformerArgs()
            {
                ReserveRawData = false,
                ExtractContentFrom = "Value",
                ExtractNameFrom = "Name"
            };
            var result = transform.Transform(data, args, out var items);

            result.ShouldBe(true);
            items.Count().ShouldBe(7);
            items.All(i => i.IsSuccess).ShouldBe(true);
            items.Where(i => i.Content == "1").Count().ShouldBe(1);
            items.Where(i => i.Content == "4").Count().ShouldBe(1);
            items.Select(i => double.Parse(i.Content)).Count(i => i >= 1).ShouldBe(4);
        }
    }
}