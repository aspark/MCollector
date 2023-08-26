using MCollector.Core.Common;
using MCollector.Core.Config;
using MCollector.Core.Contracts;
using Microsoft.Extensions.Options;
using Prometheus;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

[assembly: InternalsVisibleTo("MCollector.Test")]

namespace MCollector.Plugins.Prometheus
{
    public class PrometheusExporter : IExporter, IDisposable, IAsSingleton, IObserver<CollectedData>
    {
        //private CollectorConfig _config = null;
        private ICollectedDataPool _dataPool = null;
        ITransformerRunner _transformerRunner;

        public PrometheusExporter(ICollectedDataPool dataPool, ITransformerRunner transformerRunner)//IOptions<CollectorConfig> config, 
        {
            //_config = config.Value;
            _dataPool = dataPool;
            _transformerRunner = transformerRunner;
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
            var proConfig = SerializerHelper.CreateFrom<CollectorPrometheusConfig>(args); //_config.Exporter?.GetConfig<CollectorPrometheusConfig>(this.Name);

            if (proConfig?.Enable == true)
            {
                Metrics.SuppressDefaultMetrics();
                _gloableLables = GetLableNames(proConfig.Labels);
                _server = new MetricServer(port: proConfig?.Port ?? 9090);
                _server.Start();


                _observer = _dataPool.Subscribe(this);
            }

            return Task.CompletedTask;
        }

        private string[] GetLableNames(string label)
        {
            return (label ?? "").Split(',', ';', '，', '；').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();// .Select(n => NormalizeName(n)) 写入prometheus时才合规化，不然不能映射回header了或要多次转换
        }

        private string[] _gloableLables = new string[0];
        ConcurrentDictionary<string, Gauge> _dicMetrixs = new ConcurrentDictionary<string, Gauge>();
        private async Task Update(CollectedData data)
        {
            try
            {
                CollectedData[] items = new[] { data };

                var lableNames = _gloableLables.ToArray();

                if (data.Target.Extras.TryGetCustomConfig<ExtrasPrometheusConfig>("exporter." + this.Name, out var extras))
                {
                    //target传入的参数
                    
                    //该target不上报
                    if (extras?.Args?.Enable == false)
                        return;

                    if(extras?.Args.Labels?.Any() == true)
                    {
                        //与全局label组装
                        lableNames = lableNames.Concat(GetLableNames(extras.Args.Labels)).ToArray();
                    }

                    if (extras?.Transform?.Any() == true)
                    {
                        //再次转换数据
                        items = (await _transformerRunner.Transform(data.Target, items, extras.Transform)).ToArray();
                    }
                }

                foreach (var item in items)
                {
                    var gauge = _dicMetrixs.GetOrAdd(item.Name, k => Metrics.CreateGauge(NormalizeName(item.Name), item.Name, lableNames.Select(n => NormalizeName(n)).ToArray()));//名称可能非法

                    //如果内容是数据，侧用数据上报
                    if (item.IsSuccess && item.Content != null)
                    {
                        if (double.TryParse(item.Content, out double d))
                        {
                            gauge.Set(d);
                        }
                        else if (string.Equals(item.Content, "true", StringComparison.InvariantCultureIgnoreCase))
                        {
                            gauge.Set(1);
                        }
                        else if (string.Equals(item.Content, "false", StringComparison.InvariantCultureIgnoreCase))
                        {
                            gauge.Set(0);
                        }
                        else
                        {
                            gauge.Set(item.IsSuccess ? 1 : 0);
                        }
                    }
                    else
                    {
                        gauge.Set(item.IsSuccess ? 1 : 0);
                    }

                    //lable values
                    if (lableNames.Any() && item.Headers?.Any() == true)
                    {
                        var labelValues = lableNames.Select(l => item.Headers.ContainsKey(l) ? (item.Headers[l]?.ToString() ?? "") : "").ToArray();

                        gauge.WithLabels(labelValues);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            //移除失效的标准
            RemoveExpiredKeysAsync();

            //    var count = Metrics.CreateCounter("sbc_count", "测试计数");
            //    var histogram = Metrics.CreateHistogram("sbc_histogram", "直方图", new HistogramConfiguration
            //    {
            //        Buckets = Histogram.LinearBuckets(20, 30, 10)
            //    });

            //    var rnd = new Random(Guid.NewGuid().GetHashCode());
        }

        private string NormalizeName(string name)
        {
            return Regex.Replace(name ?? "", "[^a-zA-Z0-9_]", "_");
        }

        private Task _removeKeysTask = Task.CompletedTask;
        private void RemoveExpiredKeysAsync()
        {
            Interlocked.CompareExchange(ref _removeKeysTask, Task.Run(() =>
            {
                var exists = new HashSet<string>(_dataPool.GetData().Select(d => d.Name));
                foreach (var key in _dicMetrixs.Keys)
                {
                    if (exists.Contains(key) == false)
                    {
                        _dicMetrixs.TryRemove(key, out _);
                    }
                }

                Interlocked.Exchange(ref _removeKeysTask, Task.CompletedTask);
            }), Task.CompletedTask);
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
