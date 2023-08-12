using MCollector.Core.Config;
using MCollector.Core.Contracts;
using MCollector.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace MCollector.Core
{
    public class CollectorStarter : IAsSingleton, IDisposable
    {
        ILogger _logger;
        CollectorConfig _config;
        Dictionary<string, ICollector> _collectors = new Dictionary<string, ICollector>(StringComparer.InvariantCultureIgnoreCase);
        Dictionary<string, ITransformer> _transforms = new Dictionary<string, ITransformer>(StringComparer.InvariantCultureIgnoreCase);

        ICollectTargetManager _targetManager;
        DefaultCollectedDataPool _dataPool;
        CollectorSignal _collectorSignal;
        IList<IExporter> _exporters;

        CancellationTokenSource _tokenSource;

        public CollectorStarter(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, 
            IOptions<CollectorConfig> config, ICollectTargetManager targetManager,  IList<ICollector> collectors, IList<ITransformer> transformers, IList<IExporter> exporters)//DefaultCollectedDataAccessor dataAccessor, CollectorSignal collectorSignal
        {
            _logger = loggerFactory.CreateLogger<CollectorStarter>();

            _config = config.Value;
            _targetManager = targetManager;

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

            _dataPool = serviceProvider.GetRequiredService<DefaultCollectedDataPool>();
            _collectorSignal = serviceProvider.GetRequiredService<CollectorSignal>();

            _exporters = exporters;
        }
        public CancellationToken CancellationToken { get => _tokenSource.Token; }


        public void Dispose()
        {
            _isRunning = false;
            _tokenSource?.Cancel();
            _collectorSignal.Continue();

            foreach (var exporter in _exporters)
            {
                exporter.Stop();
            }

            Task.WaitAll(_tasks.Values.Select(v => v.Task).ToArray(), 300);

            _tokenSource?.Dispose();
            _collectorSignal.Dispose();
        }

        public void Start()
        {
            _tokenSource = new CancellationTokenSource();
            _isRunning = true;
            
            foreach (var target in _targetManager.GetAll())
            {
                StartImpl(target, CollectTargetChangedType.Add);
            }

            _targetManager.AddChangedCallback(StartImpl);

            foreach (var exporter in _exporters)
            {
                exporter.Start(_config.Exporter.ContainsKey(exporter.Name) ? _config.Exporter[exporter.Name] : new Dictionary<string, object>());
            }
        }

        bool _isRunning = false;

        private class TargetRunnerInfo : IDisposable
        {
            CancellationTokenSource _cts;
            CollectorStarter _starter;
            DefaultCollectedDataPool _dataPool;
            public TargetRunnerInfo(CollectorStarter starter, DefaultCollectedDataPool dataPool, CollectTarget target)
            {
                _cts = new CancellationTokenSource();
                Target = target;
                _starter = starter;
                _dataPool = dataPool;
                _starter.CancellationToken.Register(() => _cts.Cancel());
            }

            public CollectTarget Target { get; private set; }

            private volatile bool _isAlive = true;
            public bool IsAlive { get => _isAlive; }

            public CancellationToken CancellationToken { get => _cts.Token; }

            public Task Task { get; set; }

            //public TargetRunnerInfo Start()
            //{
            //    Task = Task.Run(() => _starter.StartAsync(this), CancellationToken);

            //    return this;
            //}

            public void Dispose()
            {
                _isAlive = false;
                _cts.Cancel();
                _dataPool.Remove(Target);
            }
        }

        private ConcurrentDictionary<string, TargetRunnerInfo> _tasks = new ConcurrentDictionary<string, TargetRunnerInfo>();
        private void StartImpl(CollectTarget target, CollectTargetChangedType type)
        {
            if (_isRunning)
            {
                if(type == CollectTargetChangedType.Delete)
                {
                    if (_tasks.TryRemove(target.Name, out TargetRunnerInfo old))
                    {
                        old.Dispose();
                    }
                }
                else
                {
                    var runnerInfo = new TargetRunnerInfo(this, _dataPool, target);

                    _tasks.AddOrUpdate(target.Name, k => StartImpl(runnerInfo), (k, o) => {
                        o.Dispose();

                        return StartImpl(runnerInfo);
                    });
                }
            }
        }

        private TargetRunnerInfo StartImpl(TargetRunnerInfo info)
        {
            var task = Task.Run(() => StartAsync(info), info.CancellationToken);
            info.Task = task;

            return info;
        }

        private async Task StartAsync(TargetRunnerInfo info)
        {
            if (_collectors.ContainsKey(info.Target.Type))
            {
                var stopwatch = new Stopwatch();
                while (_isRunning && info.IsAlive)
                {
                    stopwatch.Restart();
                    IEnumerable<CollectedData> items;
                    try
                    {
                        stopwatch.Start();

                        var data = await _collectors[info.Target.Type].Collect(info.Target);
                        data.Duration = stopwatch.ElapsedMilliseconds;
                        data.LastCollectTime = DateTime.Now;
                        items = new [] { data };

                        items = await Transform(items, info.Target.Transform);

                        if(info.IsAlive)
                        {
                            //push to results
                            _dataPool.AddOrUpdate(info.Target, items);

                            //await Task.Delay(target.Interval);
                            _collectorSignal.Wait(info.Target.GetInterval());
                        }
                    }
                    catch(Exception ex) 
                    {
                        items = new[] { new CollectedData(info.Target.Name, info.Target) { IsSuccess = false, Content = ex.Message } };
                    }
                }
            }
            else
            {
                _logger.LogWarning($"不存在{info.Target.Type}的执行器");
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
