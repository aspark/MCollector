using MCollector.Core.Common;
using MCollector.Core.Contracts;
using MCollector.Core.Transformers;
using MCollector.Plugins.Prometheus;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCollector.Test
{
    public class SerializerHelperTest
    {
        [Fact]
        public void TestCreateFromJsonElement()
        {
            var strJson = "{\"name\":\"test\", \"args\":{\"p1\":1, \"p2\":2}}";
            var dic = JsonSerializer.Deserialize<Dictionary<string, object>>(strJson);
            dic.Count.ShouldBe(2);
            dic.First().Value.GetType().IsAssignableTo(typeof(JsonElement)).ShouldBeTrue();

            var obj = SerializerHelper.CreateFrom<CollectTarget>(dic);
            obj.ShouldNotBeNull();
            obj.Name.ShouldBe("test");
            obj.Args.Count.ShouldBe(2);
            obj.Interval.ShouldBe(new CollectTarget().Interval);//default value
        }

        [Fact]
        public void TestCreateFromDic()
        {
            var dic = new Dictionary<string, object>()
            {
                {"name", "test" },
                {"args", new Dictionary<string, object>(){
                    {"p1", 1 },
                    {"p2", 2}
                    }
                },
            };

            var obj = SerializerHelper.CreateFrom<CollectTarget>(dic);
            obj.ShouldNotBeNull();
            obj.Name.ShouldBe("test");
            obj.Args.Count.ShouldBe(2);
            obj.Interval.ShouldBe(new CollectTarget().Interval);//default value
        }


        [Fact]
        public void TestCreateFromObject()
        {
            var str = @"
      prometheus:
        args: 
          p1: 1
          p2: 2
        transform: 
          search:
            text: 1
      es:
        args: 
          p1: 1
          p2: 2
        transform: 
          search:
            text: 1
";
            var dic = SerializerHelper.Deserialize<Dictionary<string, object>>(str);
            dic.Count.ShouldBe(2);
            dic.ContainsKey("prometheus").ShouldBeTrue();

            SerializerHelper.TryCreateFrom<ExtrasPrometheusConfig>(dic, "prometheus", out var obj).ShouldBe(true);
            obj.ShouldNotBeNull();
            obj.Args.Count.ShouldBe(2);
            obj.Transform.Count.ShouldBe(1);
            obj.Transform.First().Key.ShouldBe("search");

            SerializerHelper.TryCreateFrom<SearchTransformerArgs>(dic, "prometheus.transform.search", out var searchArgs).ShouldBe(true);
            searchArgs.ShouldNotBeNull();
            searchArgs.Text.ShouldBe("1");
        }


        [Fact]
        public void TestDeserilizeFromJsonString()
        {
            var strJson = "{\"name\":\"test\", \"args\":{\"p1\":1, \"p2\":2}}";
            var obj = SerializerHelper.Deserialize<CollectTarget>(strJson);
            obj.ShouldNotBeNull();
            obj.Name.ShouldBe("test");
            obj.Args.Count.ShouldBe(2);
            obj.Args.First().Value.ToString().ShouldBe("1");
            obj.Interval.ShouldBe(new CollectTarget().Interval);//default value
        }

        [Fact]
        public void TestDeserilizeFromYmlString()
        {
            var strYml = @"
name: test
target: ""localhost""
type: es.i
args:
    username: uid
    password: pwd
";
            var obj = SerializerHelper.Deserialize<CollectTarget>(strYml);
            obj.ShouldNotBeNull();
            obj.Name.ShouldBe("test");
            obj.Args.Count.ShouldBe(2);
            obj.Args.First().Value.ShouldBe("uid");
            obj.Interval.ShouldBe(new CollectTarget().Interval);//default value
        }

        [Fact]
        public void TestTryGetObject()
        {
            var str = @"
      prometheus:
        args: 
          p1: 1
          p2: 2
        transform: 
          search:
            text: 1
      es:
        args: 
          p1: 1
          p2: 2
        transform: 
          search:
            text: 1
";

            var obj = SerializerHelper.Deserialize<Dictionary<string, object>>(str);
            obj.Count.ShouldBe(2);
            obj.First().Key.ShouldBe("prometheus");
            obj.First().Value.GetType().IsAssignableTo(typeof(IDictionary<,>));//yml default type

            SerializerHelper.TryGetObject(obj, "abc", out var result).ShouldBe(false);

            SerializerHelper.TryGetObject(obj, "prometheus", out result).ShouldBe(true);
            ((object)result).GetType().IsAssignableTo(typeof(IDictionary<object, object>)).ShouldBe(true);
            ((IDictionary<object, object>)result).Count.ShouldBe(2);
            ((IDictionary<object, object>)result).ContainsKey("args").ShouldBeTrue();
            ((IDictionary<object, object>)result).ContainsKey("transform").ShouldBeTrue();

            SerializerHelper.TryGetObject(obj, "prometheus.args", out result).ShouldBe(true);
            result.GetType().IsAssignableTo(typeof(IDictionary<object, object>));
            ((IDictionary<object, object>)result).Count.ShouldBe(2);

            SerializerHelper.TryGetObject(obj, "prometheus.transform.search", out result).ShouldBe(true);
            result.GetType().IsAssignableTo(typeof(IDictionary<object, object>));
            ((IDictionary<object, object>)result).Count.ShouldBe(1);
            ((IDictionary<object, object>)result).First().Value.ToString().ShouldBe("1");
        }
    }
}
