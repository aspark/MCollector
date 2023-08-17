using MCollector.Core.Common;
using MCollector.Core.Contracts;

namespace MCollector.Core
{
    public interface ICollectorSignal
    {
        void Continue();

        bool Wait(int timeout);
    }

    internal class CollectorSignal : IAsSingleton, IDisposable, ICollectorSignal
    {
        private ManualResetEvent _sign = new ManualResetEvent(false);

        public CollectorSignal()
        {
            
        }

        public bool Wait(int timeout)
        {
            return _sign.WaitOne(timeout);
        }

        ManualResetEvent _last = null;
        public void Continue()
        {
            var dispose = _last;
            TaskHelper.DelayAsyncRun(() => dispose?.Dispose(), 100);//避免Continue被连续调时用，上一个last还没set完就dispose了

            _last = Interlocked.Exchange(ref _sign, new ManualResetEvent(false));
            _last.Set();
        }

        public void Dispose()
        {
            _sign?.Dispose();
        }
    }
}
