using System.Collections.Concurrent;
using MCollector.Core.Common;
using MCollector.Core.Contracts;

namespace MCollector.Core.Results
{
    internal class DefaultCollectedDataPool : ICollectedDataPool, IAsSingleton, IDisposable
    {
        private ConcurrentDictionary<CollectTarget, IEnumerable<CollectedData>> _collectedData = new ConcurrentDictionary<CollectTarget, IEnumerable<CollectedData>>();

        public DefaultCollectedDataPool()
        {
            
        }

        public void Dispose()
        {
            _collectedData.Clear();
        }

        public IList<CollectedData> GetData()
        {
            return _collectedData.Values.SelectMany(v => v).ToList().AsReadOnly();
        }

        public event EventHandler<IEnumerable<CollectedData>> DataChanged;

        private List<IObserver<CollectedData>> _observers = new List<IObserver<CollectedData>>();

        public IDisposable Subscribe(IObserver<CollectedData> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);

            return new DisposableCallback(() => _observers.Remove(observer));
        }

        internal void AddOrUpdate(CollectTarget target, IEnumerable<CollectedData> items)
        {
            if(items?.Any() == true)
            {
                _collectedData.AddOrUpdate(target, items, (k, o) => items);

                _observers.ForEach(o =>
                {
                    Task.Run(() =>
                    {
                        foreach (var item in items)
                        {
                            o.OnNext(item);
                        }
                    });
                });


                Task.Run(() => DataChanged?.Invoke(this, items));
            }
        }

        public void Remove(CollectTarget target)
        {
            _collectedData.TryRemove(target, out _);
        }
    }
}
