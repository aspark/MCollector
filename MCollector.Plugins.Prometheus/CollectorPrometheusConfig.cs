using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Plugins.Prometheus
{
    internal class CollectorPrometheusConfig
    {
        public bool Enable { get; set; } = true;

        /// <summary>
        /// 暴露的接口
        /// </summary>
        public int Port { get; set; }

        public string Labels { get; set; } = string.Empty;
    }
}
