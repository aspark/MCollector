using MCollector.Core.Common;
using MCollector.Core.Config;
using MCollector.Core.Contracts;
using Microsoft.Extensions.Options;
using Prometheus;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace MCollector.Plugins.Prometheus
{
    internal class PrometheusExporter : IExporter, IDisposable, IAsSingleton, IObserver<CollectedData>
    {
        private CollectorConfig _config = null;
        private ICollectedDataPool _resultAccessor = null;

        public PrometheusExporter(IOptions<CollectorConfig> config, ICollectedDataPool resultAccessor)
        {
            _config = config.Value;
            _resultAccessor = resultAccessor;
        }

        public void Dispose()
        {
            _observer?.Dispose();
            _server?.Dispose();
        }

        public string Name => "prometheus";

        MetricServer _server;
        IDisposable _observer;
        public Task Start(Dictionary<string, object> args)
        {
            var proConfig = SerializerHelper.Deserialize<CollectorPrometheusConfig>(args); //_config.Exporter?.GetConfig<CollectorPrometheusConfig>(this.Name);

            if (proConfig?.Enable == true)
            {
                Metrics.SuppressDefaultMetrics();

                _server = new MetricServer(port: proConfig?.Port ?? 9090);
                _server.Start();


                _observer = _resultAccessor.Subscribe(this);
            }

            return Task.CompletedTask;
        }


        ConcurrentDictionary<string, Gauge> _dicMetrixs = new ConcurrentDictionary<string, Gauge>();
        private void Update(CollectedData data)
        {
            var gauge = _dicMetrixs.GetOrAdd(data.Name, k => Metrics.CreateGauge(Regex.Replace(data.Name, "[^a-zA-Z0-9_]", "_"), data.Name));//名称可能非法

            //如果内容是数据，侧用数据上报
            if (data.Content != null)
            {
                if(double.TryParse(data.Content, out double d))
                {
                    gauge.Set(d);
                }
                else if (string.Equals(data.Content, "true", StringComparison.InvariantCultureIgnoreCase))
                {
                    gauge.Set(1);
                }
                else if (string.Equals(data.Content, "false", StringComparison.InvariantCultureIgnoreCase))
                {
                    gauge.Set(0);
                }
                else
                {
                    gauge.Set(data.IsSuccess ? 1 : 0);
                }
            }
            else
            {
                gauge.Set(data.IsSuccess ? 1 : 0);
            }

            //    var count = Metrics.CreateCounter("sbc_count", "测试计数");
            //    var histogram = Metrics.CreateHistogram("sbc_histogram", "直方图", new HistogramConfiguration
            //    {
            //        Buckets = Histogram.LinearBuckets(20, 30, 10)
            //    });

            //    var rnd = new Random(Guid.NewGuid().GetHashCode());
        }

        public Task Stop()
        {
            Dispose();

            return Task.CompletedTask;
        }

        public void OnCompleted()
        {
            
        }

        public void OnError(Exception error)
        {
            
        }

        public void OnNext(CollectedData value)
        {
            Update(value);
        }
    }
}
