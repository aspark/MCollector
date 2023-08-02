namespace MCollector.Core.Common
{
    internal class DisposableCallback : IDisposable
    {
        private Action _callback = null;

        public DisposableCallback(Action callback)
        {
            _callback = callback;
        }

        public void Dispose()
        {
            _callback?.Invoke();
        }
    }
}
