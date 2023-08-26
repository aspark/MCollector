using MCollector.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Plugins.Prometheus
{
    internal class ExtrasPrometheusConfig
    {
        public Dictionary<string, object> Args { get; set; } = new Dictionary<string, object>();

        public TransferConfig Transform { get; set; } = new TransferConfig();
    }
}
