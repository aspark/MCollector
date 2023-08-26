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
        public int Port { get; set; } = 18086;

        public CollectorApiConfig Api { get; set; }

        public CollectorExporterConfig Exporter { get; set; }

        /// <summary>
        /// 检测集合
        /// </summary>
        public CollectTarget[] Targets { get; set; }

        ///// <summary>
        ///// 引用的公共配置，主要用于prepare/tranform等使用
        ///// 可使用yaml自带的引用
        ///// </summary>
        //public CollectorRefConfig Refs { get; set; } = new CollectorRefConfig();

        //public T ConvertTo<T>(Dictionary<string, object> parameters) where T : class, new()
        //{
        //    var config = SerializerHelper.Deserialize<T>(parameters);

        //    return config;
        //}
    }


    public class CollectorApiConfig
    {
        public bool Status { get; set; } = true;

        public bool StatusContainsSuccessDetails { get; set; } = false;

        public bool Refresh { get; set;} = true;
    }

    //public class CollectorRefConfig: Dictionary<string, Dictionary<string , object>>
    //{
    //    //public string Name { get; set; }

    //    //public Dictionary<string, object> Data { get; set; }
    //}
}
