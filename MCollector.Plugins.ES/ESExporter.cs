using MCollector.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Plugins.ES
{
    internal class ESExporter : IExporter, IDisposable, IAsSingleton, IObserver<CollectedData>
    {
        public string Name => "es";

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(CollectedData value)
        {
            throw new NotImplementedException();
        }

        public Task Start(Dictionary<string, object> args)
        {
            throw new NotImplementedException();
        }

        public Task Stop()
        {
            throw new NotImplementedException();
        }
    }
}
