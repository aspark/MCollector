using MCollector.Core.Contracts;

namespace MCollector.Plugins.K8s
{
    public class K8sContainerCollector : ICollector
    {
        public string Type => "k8s.c";

        public Task<CollectedData> Collect(CollectTarget target)
        {
            throw new NotImplementedException();
        }
    }
}