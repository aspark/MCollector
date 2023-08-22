using MCollector.Core.Config;
using MCollector.Core.Contracts;
using MCollector.Core.Transformers;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCollector.Test
{
    public class TargetsTransformTest
    {
        [Fact]
        public async Task TestYmlTargets()
        {
            var targetManager = new DefaultCollectTargetManager(Options.Create(new CollectorConfig() { Targets = new CollectTarget[0] }));
            var mProtector = new Mock<IProtector>();
            mProtector.Setup(p => p.Protect(It.IsAny<string>())).Returns((string s) => s);
            mProtector.Setup(p => p.Unprotect(It.IsAny<string>())).Returns((string s) => s);
            mProtector.Setup(p => p.FindAndUnprotectText(It.IsAny<string>())).Returns((string s) => s);
            mProtector.Setup(p => p.FindProtectedText(It.IsAny<string>())).Returns(new string[0]);

            var transform = new MCTargetsTransformer(targetManager, mProtector.Object);

            var target = new CollectTarget()
            {
                Name = "test"
            };

            var data = new CollectedData(target.Name, target)
            {
                Content = File.ReadAllText("contents/target-yml.txt")
            };

            var result = await transform.Transform(data, new TargetsTransformerArgs() { });

            result.IsSuccess.ShouldBe(true);
            result.Items.Count().ShouldBe(1);// add sucessful
            targetManager.GetAll().Count.ShouldBe(2);
            targetManager.GetAll().Where(i => i.Name.Equals("oauth", StringComparison.InvariantCultureIgnoreCase)).Count().ShouldBe(1);
            targetManager.GetAll().First(i => i.Name.Equals("oauth", StringComparison.InvariantCultureIgnoreCase)).Prepare.Count().ShouldBe(1);
            targetManager.GetAll().First(i => i.Name.Equals("oauth", StringComparison.InvariantCultureIgnoreCase)).Prepare.First().Value.Count().ShouldBe(3);
            targetManager.GetAll().First(i => i.Name.Equals("oauth", StringComparison.InvariantCultureIgnoreCase)).Prepare.First().Value["clientSecret"].ShouldBe("test");
        }

        [Fact]
        public async Task TestJsonTargets()
        {
            var targetManager = new DefaultCollectTargetManager(Options.Create(new CollectorConfig() { Targets = new CollectTarget[0] }));
            var mProtector = new Mock<IProtector>();
            mProtector.Setup(p => p.Protect(It.IsAny<string>())).Returns((string s) => s);
            mProtector.Setup(p => p.Unprotect(It.IsAny<string>())).Returns((string s) => s);
            mProtector.Setup(p => p.FindAndUnprotectText(It.IsAny<string>())).Returns((string s) => s);
            mProtector.Setup(p => p.FindProtectedText(It.IsAny<string>())).Returns(new string[0]);

            var transform = new MCTargetsTransformer(targetManager, mProtector.Object);

            var target = new CollectTarget()
            {
                Name = "test"
            };

            var data = new CollectedData(target.Name, target)
            {
                Content = File.ReadAllText("contents/target-json.txt")
            };

            var result = await transform.Transform(data, new TargetsTransformerArgs() {
                 RootPath = "targets"
            });

            result.IsSuccess.ShouldBe(true);
            result.Items.Count().ShouldBe(1);// add sucessful
            targetManager.GetAll().Count.ShouldBe(2);
            targetManager.GetAll().Where(i => i.Name.Equals("localhost metrics", StringComparison.InvariantCultureIgnoreCase)).Count().ShouldBe(1);
            targetManager.GetAll().First(i => i.Name.Equals("localhost metrics", StringComparison.InvariantCultureIgnoreCase)).Transform.Count().ShouldBe(1);
            targetManager.GetAll().First(i => i.Name.Equals("localhost metrics", StringComparison.InvariantCultureIgnoreCase)).Transform.First().Value.Count().ShouldBe(2);
            targetManager.GetAll().First(i => i.Name.Equals("localhost metrics", StringComparison.InvariantCultureIgnoreCase)).Transform.First().Value["extractNameFrom"].GetType().IsAssignableTo(typeof(JsonElement)).ShouldBe(true);//convert args 有类型判断
            targetManager.GetAll().First(i => i.Name.Equals("localhost metrics", StringComparison.InvariantCultureIgnoreCase)).Transform.First().Value["extractNameFrom"].ToString().ShouldBe("Name");
        }
    }
}
