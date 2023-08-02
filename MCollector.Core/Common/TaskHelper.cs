namespace MCollector.Core.Common
{
    public class TaskHelper
    {
        public static Task DelayAsyncRun(Action callback, int timeout)
        {
            return Task.Run(async () =>
            {
                await Task.Delay(timeout);
                callback?.Invoke();
            });
        }
    }
}
