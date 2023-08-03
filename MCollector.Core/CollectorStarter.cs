using MCollector.Core.Config;
using MCollector.Core.Contracts;
using MCollector.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace MCollector.Core
{
    public class CollectorStarter : IAsSingleton, IDisposable
    {
        ILogger _logger;
        CollectorConfig _config;
        Dictionary<string, ICollector> _collectors = new Dictionary<string, ICollector>(StringComparer.InvariantCultureIgnoreCase);
        Dictionary<string, ITransformer> _transforms = new Dictionary<string, ITransformer>(StringComparer.InvariantCultureIgnoreCase);

        DefaultCollectedDataAccessor _dataAccessor;
        CollectorSignal _collectorSignal;
        IList<IExporter> _exporters;

        CancellationTokenSource _tokenSource;

        public CollectorStarter(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, 
            IOptions<CollectorConfig> config,  IList<ICollector> collectors, IList<ITransformer> transformers, IList<IExporter> exporters)//DefaultCollectedDataAccessor dataAccessor, CollectorSignal collectorSignal
        {
            _logger = loggerFactory.CreateLogger<CollectorStarter>();

            _config = config.Value;

            if (!collectors.Any())
            {
                throw new ArgumentException(nameof(collectors) + "不可为空");
            }

            foreach (var collector in collectors)
            {
                _collectors[collector.Type] = collector;
            }

            foreach (var tranformer in transformers)
            {
                _transforms[tranformer.Name] = tranformer;
            }

            _dataAccessor = serviceProvider.GetRequiredService<DefaultCollectedDataAccessor>();
            _collectorSignal = serviceProvider.GetRequiredService<CollectorSignal>();

            _exporters = exporters;
        }

        public void Dispose()
        {
            foreach (var exporter in _exporters)
            {
                exporter.Stop();
            }

            _tokenSource?.Cancel();
            _isRunning = false;
            Task.WaitAll(_tasks.ToArray(), 300);
            _tokenSource?.Dispose();
        }

        private List<Task> _tasks = new List<Task>();
        public void Start()
        {
            _tokenSource = new CancellationTokenSource();
            _isRunning = true;
            foreach (var target in _config.Targets)
            {
                _tasks.Add(Task.Run(() => StartImpl(target), _tokenSource.Token));
            }

            foreach (var exporter in _exporters)
            {
                exporter.Start(_config.Exporter.ContainsKey(exporter.Name) ? _config.Exporter[exporter.Name] : new Dictionary<string, object>());
            }
        }

        bool _isRunning = false;
        private async Task StartImpl(CollectTarget target)
        {
            if (_collectors.ContainsKey(target.Type))
            {
                var stopwatch = new Stopwatch();
                while (_isRunning)
                {
                    stopwatch.Restart();
                    IEnumerable<CollectedData> items;
                    try
                    {
                        stopwatch.Start();

                        var data = await _collectors[target.Type].Collect(target);
                        data.Duration = stopwatch.ElapsedMilliseconds;
                        data.LastCollectTime = DateTime.Now;
                        items = new [] { data };

                        items = await Transform(items, target.Transform);

                        //push to results
                        _dataAccessor.AddOrUpdate(target, items);

                        //await Task.Delay(target.Interval);
                        _collectorSignal.Wait(target.GetInterval());
                    }
                    catch(Exception ex) 
                    {
                        items = new[] { new CollectedData(target.Name, target) { IsSuccess = false, Content = ex.Message } };
                    }
                }
            }
            else
            {
                _logger.LogWarning($"不存在{target.Type}的执行器");
            }
        }

        private async Task<IEnumerable<CollectedData>> Transform(IEnumerable<CollectedData> items, Dictionary<string, Dictionary<string, object>> transformers)
        {
            if (transformers?.Any() == true)
            {
                var changedItems = new List<CollectedData>();
                foreach(var item in items)
                {
                    foreach (var trans in transformers)
                    {
                        if (item is FinalCollectedData || !item.IsSuccess)
                        {
                            changedItems.Add(item);

                            break;
                        }

                        if (_transforms.ContainsKey(trans.Key))
                        {
                            changedItems.AddRange(await _transforms[trans.Key].Run(new[] { item } , trans.Value));
                        }
                    }
                }

                return changedItems;
            }

            return items;
        }
    }
}
