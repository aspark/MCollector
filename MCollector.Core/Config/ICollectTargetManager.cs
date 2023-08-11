using MCollector.Core.Common;
using MCollector.Core.Contracts;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Core.Config
{
    public interface ICollectTargetManager: IAsSingleton
    {
        public IReadOnlyList<CollectTarget> GetAll();

        public bool Merge(IList<CollectTarget> list);

        public IDisposable AddChangedCallback(Action<CollectTarget, CollectTargetChangedType> callback);
    }

    public enum CollectTargetChangedType
    {
        Update,
        Delete,
        Add
    }

    internal class DefaultCollectTargetManager: ICollectTargetManager
    {
        CollectorConfig _config;

        ConcurrentDictionary<string, CollectTarget> _targets;

        public DefaultCollectTargetManager(IOptions<CollectorConfig> config)
        {
            _config = config.Value;
            _targets = new ConcurrentDictionary<string, CollectTarget>();
            //foreach(var target in _config.Targets)
            //{
            //    _targets[target.Name] = target;
            //}

            Merge(_config.Targets);
        }

        //以Name为准，所以远程的会覆盖本址的target
        public bool Merge(IList<CollectTarget> list)
        {
            if (list?.Any() == true)
            {
                var newTrace = new HashSet<string>(list.Select(i => i.Trace));
                var newNames = new HashSet<string>(list.Select(i => i.Name));
                //同相来源的，如果不在Merge list中，则会移除
                foreach (var deleted in _targets.Values.Where(v => newTrace.Contains(v.Trace) && !newNames.Contains(v.Name)))
                {
                    Callback(deleted, CollectTargetChangedType.Delete);
                }

                foreach (var item in list)
                {
                    if (_targets.ContainsKey(item.Name))
                    {
                        if(_targets.TryGetValue(item.Name, out CollectTarget original))
                        {
                            if (original != null && original.GetVersion() != item.GetVersion())
                            {
                                if (_targets.TryUpdate(item.Name, item, original))
                                {
                                    Callback(item, CollectTargetChangedType.Update);
                                }
                            }
                        }
                    }
                    else
                    {
                        if(_targets.TryAdd(item.Name, item))
                        {
                            Callback(item, CollectTargetChangedType.Add);
                        }
                    }
                }

                return true;
            }

            return false;
        }

        private ConcurrentDictionary<Action<CollectTarget, CollectTargetChangedType>, IDisposable> _callbacks = new ConcurrentDictionary<Action<CollectTarget, CollectTargetChangedType>, IDisposable>();
        public IDisposable AddChangedCallback(Action<CollectTarget, CollectTargetChangedType> callback)
        {
            if (_callbacks.TryGetValue(callback, out IDisposable exist))
                return exist;

            var disposable = new DisposableCallback(() => { 
                _callbacks.TryRemove(callback, out _);
            });

            _callbacks[callback] = disposable;

            return disposable;
        }

        private void Callback(CollectTarget target, CollectTargetChangedType type)
        {
            if (_callbacks.Any() == false)
                return;

            Task.Run(() =>
            {
                foreach (var callback in _callbacks.Keys.ToArray())
                {
                    callback(target, type);
                }
            });
        }

        public IReadOnlyList<CollectTarget> GetAll()
        {
            return _targets.Values.ToArray().AsReadOnly();
        }
    }
}
