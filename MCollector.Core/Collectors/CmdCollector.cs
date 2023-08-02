using MCollector.Core.Contracts;

namespace MCollector.Core.Collectors
{
    internal class CmdCollector : ICollector
    {
        public string Type => "cmd";

        public Task<CollectedData> Collect(CollectTarget target)
        {
            return Task.FromResult(new CollectedData(target.Name, target) { IsSuccess = false, Content = "未实现该采集器" });
        }
    }
}
