using MCollector.Core.Common;
using MCollector.Core.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MCollector.Core.Config
{
    public class CollectorConfig
    {
        /// <summary>
        /// 暴露的端口
        /// </summary>
        public int Port { get; set; } = 18080;

        public CollectorApiConfig Api { get; set; }

        public CollectorExporterConfig Exporter { get; set; }

        /// <summary>
        /// 检测集合
        /// </summary>
        public CollectTarget[] Targets { get; set; }

        //public T ConvertTo<T>(Dictionary<string, object> parameters) where T : class, new()
        //{
        //    var config = SerializerHelper.Deserialize<T>(parameters);

        //    return config;
        //}
    }


    public class CollectorApiConfig
    {
        public bool Status { get; set; }= true;

        public bool Refresh { get; set;} = true;
    }
}
