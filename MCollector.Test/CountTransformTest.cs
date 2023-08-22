using MCollector.Core.Contracts;
using MCollector.Core.Transformers;

namespace MCollector.Test
{
    public class CountTransformTest
    {
        [Fact]
        public async Task TestDefault()
        {
            var transform = new CountTransformer();

            var target = new CollectTarget() { 
                Name = "test"
            };

            var items = new[] {
                new CollectedData(target.Name, target)
                {
                    Content = "abc"
                }, new CollectedData(target.Name, target)
                {
                    Content = "def"
                }
            };

            var args = new Dictionary<string, object>() { };

            var result = (await transform.Run(target, items, args)).ToList();

            result.Count.ShouldBe(1);
            result.First().Content.ShouldBe("2");
        }


        [Fact]
        public async Task TestKeepMode()
        {
            var transform = new CountTransformer();

            var target = new CollectTarget()
            {
                Name = "test"
            };

            var items = new[] {
                new CollectedData(target.Name, target)
                {
                    Content = "abc"
                }, new CollectedData(target.Name, target)
                {
                    Content = "def"
                }
            };

            var args = new Dictionary<string, object>() {
                {"mode", "keep" } 
            };

            var result = (await transform.Run(target, items, args)).ToList();

            result.Count.ShouldBe(2);
            result.First().Content.ShouldBe("1");
            result.First().Content.ShouldBe("1");
        }

        [Fact]
        public async Task TestAsJsonMode()
        {
            var transform = new CountTransformer();

            var target = new CollectTarget()
            {
                Name = "test"
            };

            var items = new[] {
                new CollectedData(target.Name, target)
                {
                    Content = "{\"a\":1}"
                }, new CollectedData(target.Name, target)
                {
                    Content = "[{\"b\":2},{}]"
                }
            };

            var args = new Dictionary<string, object>() {
                {"asJson", true }
            };

            var result = (await transform.Run(target, items, args)).ToList();

            result.Count.ShouldBe(1);
            result.First().Content.ShouldBe("3");
        }
    }
}