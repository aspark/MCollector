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
        public CollectorPrometheusConfig Args { get; set; }

        public TransferConfig Transform { get; set; } = new TransferConfig();
    }
}
