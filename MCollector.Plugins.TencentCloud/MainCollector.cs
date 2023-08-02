

using MCollector.Core.Contracts;

namespace MCollector.Plugins.TencentCloud
{
    public class MainCollector : ICollector
    {
        public string Type => "oauth";

        public Task<CollectedData> Collect(CollectTarget target)
        {
            throw new NotImplementedException();
        }
    }
}