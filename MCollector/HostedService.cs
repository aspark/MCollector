using MCollector.Core;
using MCollector.Core.Config;
using MCollector.Core.Contracts;
using Microsoft.Extensions.Options;

namespace MCollector
{
    internal class HostedService : IHostedService, IDisposable
    {
        CollectorConfig _config = null;
        CollectorStarter _starter = null;

        public HostedService(IOptions<CollectorConfig> config, CollectorStarter starter)
        {
            _config = config.Value;
            _starter = starter;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _starter.Start();

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _starter?.Dispose();
        }


    }
}
