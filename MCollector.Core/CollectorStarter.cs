using MCollector.Core.Config;
using MCollector.Core.Contracts;
using MCollector.Core.Results;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly:InternalsVisibleTo("MCollector.Test")]

namespace MCollector.Core
{
    public class CollectorStarter : IAsSingleton, IDisposable
    {
        ILogger _logger;
        CollectorConfig _config;
        Dictionary<string, ICollector> _collectors = new Dictionary<string, ICollector>(StringComparer.InvariantCultureIgnoreCase);
        Dictionary<string, IPreparer> _preparers = new Dictionary<string, IPreparer>(StringComparer.InvariantCultureIgnoreCase);
        Dictionary<string, IExporter> _exporters = new Dictionary<string, IExporter>();
        ITransformerRunner _transformerRunner;

        ICollectTargetManager _targetManager;
        DefaultCollectedDataPool _dataPool;
        CollectorSignal _collectorSignal;

        CancellationTokenSource _tokenSource;

        public CollectorStarter(IServiceProvider serviceProvider, ILoggerFactory loggerFactory, 
            IOptions<CollectorConfig> config, ICollectTargetManager targetManager,  IList<ICollector> collectors, ITransformerRunner transformerRunner, IList<IPreparer> preparers, IList<IExporter> exporters)//DefaultCollectedDataAccessor dataAccessor, CollectorSignal collectorSignal
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

            foreach (var preparer in preparers)
            {
                _preparers[preparer.Name] = preparer;
            }

            foreach (var exporter in exporters)
            {
                _exporters[exporter.Name] = exporter;
            }

            _transformerRunner = transformerRunner;

            _dataPool = serviceProvider.GetRequiredService<DefaultCollectedDataPool>();
            _collectorSignal = serviceProvider.GetRequiredService<CollectorSignal>();
        }
        public CancellationToken CancellationToken { get => _tokenSource.Token; }


        public void Dispose()
        {
            _isRunning = false;
            _tokenSource?.Cancel();
            _collectorSignal.Continue();

            foreach (var exporter in _exporters.Values)
            {
                exporter.Stop();
            }

            Task.WaitAll(_tasks.Values.Select(v => v.Task).ToArray(), 300);

            _tokenSource?.Dispose();
            _collectorSignal.Dispose();
        }

        public async Task Start()
        {
            _tokenSource = new CancellationTokenSource();
            _isRunning = true;

            //要先启动exporter，不然可能有数据丢失
            if (_config.Exporter?.Any() == true)
            {
                foreach (var exporterConfig in _config.Exporter)
                {
                    if (_exporters.ContainsKey(exporterConfig.Key))
                    {
                        await _exporters[exporterConfig.Key].Start(exporterConfig.Value ?? new Dictionary<string, object>());
                    }
                }
            }
            
            foreach (var target in _targetManager.GetAll())
            {
                StartCollect(target, CollectTargetChangedType.Add);
            }

            _targetManager.AddChangedCallback(StartCollect);
        }

        bool _isRunning = false;

        private class TargetCollectInfo : IDisposable
        {
            CancellationTokenSource _cts;
            CollectorStarter _starter;
            DefaultCollectedDataPool _dataPool;
            public TargetCollectInfo(CollectorStarter starter, DefaultCollectedDataPool dataPool, CollectTarget target)
            {
                _cts = new CancellationTokenSource();
                Target = target;//todo 复制一个target，因为prepare可能会修改，但也要考虑是否有引用比较
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

        private ConcurrentDictionary<string, TargetCollectInfo> _tasks = new ConcurrentDictionary<string, TargetCollectInfo>();
        private void StartCollect(CollectTarget target, CollectTargetChangedType type)
        {
            if (_isRunning)
            {
                if(type == CollectTargetChangedType.Delete)
                {
                    if (_tasks.TryRemove(target.Name, out TargetCollectInfo old))
                    {
                        old.Dispose();
                    }
                }
                else
                {
                    var runnerInfo = new TargetCollectInfo(this, _dataPool, target);

                    _tasks.AddOrUpdate(target.Name, k => StartCollect(runnerInfo), (k, o) => {
                        o.Dispose();

                        return StartCollect(runnerInfo);
                    });
                }
            }
        }

        private TargetCollectInfo StartCollect(TargetCollectInfo info)
        {
            var task = Task.Run(() => StartAsync(info), info.CancellationToken);
            info.Task = task;

            return info;
        }

        private async Task StartAsync(TargetCollectInfo info)
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

                        //prepaire
                        Prepare(info.Target, info.Target.Prepare);

                        //collect
                        var data = await _collectors[info.Target.Type].Collect(info.Target);
                        data.Duration = stopwatch.ElapsedMilliseconds;
                        data.LastCollectTime = DateTime.Now;
                        items = new [] { data };

                        //transform
                        items = await _transformerRunner.Transform(info.Target, items, info.Target.Transform);
                    }
                    catch(Exception ex) 
                    {
                        Console.WriteLine(ex.ToString());
                        items = new[] { new CollectedData(info.Target.Name, info.Target) { IsSuccess = false, Content = ex.Message } };
                    }

                    if (info.IsAlive)
                    {
                        try
                        {
                            if(items?.Any() == true)
                            {
                                //push to results
                                _dataPool.AddOrUpdate(info.Target, items);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }

                        //await Task.Delay(target.Interval);
                        _collectorSignal.Wait(info.Target.GetInterval());
                    }
                }
            }
            else
            {
                _logger.LogWarning($"不存在{info.Target.Type}的执行器");
            }
        }

        private void Prepare(CollectTarget target, Dictionary<string, Dictionary<string, object>> preparers)
        {
            if(preparers?.Any() == true)
            {
                foreach(var prepare in  preparers)
                {
                    if (_preparers.ContainsKey(prepare.Key))
                    {
                        _preparers[prepare.Key].Process(target, prepare.Value);
                    }
                }
            }

            //if()
        }
    }
}
