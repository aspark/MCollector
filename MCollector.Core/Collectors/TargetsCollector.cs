using MCollector.Core.Contracts;

namespace MCollector.Core.Collectors
{
    /// <summary>
    /// 拉取Targets
    /// </summary>
    internal class TargetsCollector : UrlCollector, ICollector
    {
        public TargetsCollector(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
        {
        }

        public override string Type => "targets";

        public override Task<CollectedData> Collect(CollectTarget target)
        {
            return Task.FromResult(new CollectedData(target.Name, target) { IsSuccess = false, Content = "未实现该采集器" });
        }
    }
}
