using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCollector.Models
{
    public class CollectedResult
    {
        public string Name { get; set; }

        /// <summary>
        /// 是否健康
        /// </summary>
        public bool IsHealth { get;set; }

        /// <summary>
        /// 执行耗时
        /// </summary>
        public long Duration { get; set; }

        /// <summary>
        /// 返回的内容
        /// </summary>
        public string Msg { get; set; }


        public DateTime LastUpdateTime { get; set; } = DateTime.Now;
    }
}
