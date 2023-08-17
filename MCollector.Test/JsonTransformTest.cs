using MCollector.Core.Contracts;
using MCollector.Core.Transformers;

namespace MCollector.Test
{
    public class JsonTransformTest
    {
        [Fact]
        public void TestJsonObjectTransform()
        {
            var transform = new JsonTransformer();

            var target = new CollectTarget() { 
                Name = "test"
            };

            var data = new CollectedData(target.Name, target) {
                Content = File.ReadAllText("contents/content-jobject.txt")
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
        public void TestJsonArrayTransform()
        {
            var transform = new JsonTransformer();

            var target = new CollectTarget()
            {
                Name = "test"
            };

            var data = new CollectedData(target.Name, target)
            {
                Content = File.ReadAllText("contents/content-jarray.txt")
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