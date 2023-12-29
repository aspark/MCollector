using MCollector.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Test
{
    public class CollectTargetTest
    {
        [Fact]
        public void Interval()
        {
            var target = new CollectTarget() { Interval = "3",  RetryInterval = "[1m,3m,5m,7m,9m]" };

            target.GetInterval().ShouldBe(3 * 1000);//3s
            target.GetInterval(true).ShouldBe(1 * 60 * 1000);//[1m,3m,5m,7m,9m]
            target.GetInterval(true).ShouldBe(3 * 60 * 1000);
            target.GetInterval(true).ShouldBe(5 * 60 * 1000);
            target.GetInterval(true).ShouldBe(7 * 60 * 1000);
            target.GetInterval(true).ShouldBe(9 * 60 * 1000);
            target.GetInterval(true).ShouldBe(1 * 60 * 1000);//循环的
            target.GetInterval(true).ShouldBe(3 * 60 * 1000);
            target.GetInterval(true).ShouldBe(5 * 60 * 1000);

            target = new CollectTarget() { Interval= "1m", RetryInterval="[3s,1h]" };
            target.GetInterval().ShouldBe(1 * 60 * 1000);
            target.GetInterval(true).ShouldBe(3 * 1000);
            target.GetInterval(true).ShouldBe(1 * 60 * 60 * 1000);
            target.GetInterval(true).ShouldBe(3 * 1000);
            target.GetInterval(true).ShouldBe(1 * 60 * 60 * 1000);
            target.GetInterval(true).ShouldBe(3 * 1000);

            target = new CollectTarget() { Interval = "rand(10s, 20s)", RetryInterval = "5m" };
            var interval = target.GetInterval();
            interval.ShouldBeLessThan(20 * 1000);
            interval.ShouldBeGreaterThanOrEqualTo(10 * 1000);

            interval = target.GetInterval();
            interval.ShouldBeLessThan(20 * 1000);
            interval.ShouldBeGreaterThanOrEqualTo(10 * 1000);

            target.GetInterval(true).ShouldBe(5 * 60 * 1000);

            target = new CollectTarget() { Interval = "rand(10s)", RetryInterval = "80s" };
            interval = target.GetInterval();
            interval.ShouldBeLessThan(11 * 1000);
            interval.ShouldBeGreaterThanOrEqualTo(9 * 1000);
            target.GetInterval(true).ShouldBe(80 * 1000);
        }
    }
}
