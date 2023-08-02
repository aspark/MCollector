using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCollector.Core.Contracts
{
    public class CollectTarget
    {
        private string _name;

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                    _name = $"{Type} {Target}";

                return _name;
            }
            set { _name = value; }
        }

        /// <summary>
        /// 检测方式
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 目标
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// 间隔时间（毫秒）
        /// </summary>
        public int Interval { get; set; } = 3000;

        /// <summary>
        /// 发送的头信息
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// 发送的内容，
        /// </summary>
        public string[]? Contents { get; set; }

        /// <summary>
        /// 对返回内容的转换器
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> Transform { get; set; }

        private string SHA1(string content)
        {
            using var hash = System.Security.Cryptography.SHA1.Create();
            return Convert.ToHexString(hash.ComputeHash(Encoding.UTF8.GetBytes(content))).ToLower();
        }
    }
}
