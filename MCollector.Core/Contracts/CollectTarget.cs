using MCollector.Core.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MCollector.Core.Contracts
{
    public class CollectTarget
    {
        private string _name;

        /// <summary>
        /// 名称，唯一标识
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

        public Dictionary<string, object> Args { get; set; }

        /// <summary>
        /// 目标
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// 间隔时间（毫秒）
        /// </summary>
        public string Interval { get; set; } = "3s";

        private Random _rand = new Random(DateTime.Now.Millisecond);
        private Func<int>? _fnInterval = null;
        //private int? _interval;
        public int GetInterval()
        {
            if (_fnInterval == null)
            {
                lock (this)
                {
                    if (_fnInterval == null)
                    {
                        if (Interval.StartsWith("rand", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Regex ex = new Regex(@"\((?<s>[^,]+),?\s*(?<e>[^\s]+)?\)");
                            var m = ex.Match(Interval);
                            if (m.Success)
                            {
                                var start = ParseTime(m.Groups["s"].Value);
                                var end = 0;
                                if (m.Groups.ContainsKey("e") && !string.IsNullOrWhiteSpace(m.Groups["e"].Value))
                                {
                                    end = ParseTime(m.Groups["e"].Value);
                                }
                                else
                                {
                                    end = (int)(start * 1.1);
                                    start = (int)(start * 0.9);
                                }

                                _fnInterval = new Func<int>(() => _rand.Next(start, end));
                            }
                        }
                        else
                        {
                            var interval = ParseTime(Interval);

                            _fnInterval = new Func<int>(() => interval);
                        }

                        if (_fnInterval == null)
                            _fnInterval = new Func<int>(() => 3000);
                    }
                }
            }

            return _fnInterval();
        }

        private int ParseTime(string time, int defaultTime = 3000)
        {
            var t = defaultTime;
            Regex ex = new Regex(@"^(?<n>[0-9\.]+)(?<u>\w{0,2})?$");
            var m = ex.Match(time);

            if (m.Success)
            {
                var num = double.Parse(m.Groups["n"].Value);

                switch (m.Groups["u"].Value.ToLower())
                {
                    case "ms":
                        t = (int)(num);
                        break;
                    case "m":
                        t = (int)(num * 60 * 1000);
                        break;
                    case "h":
                        t = (int)(num * 60 * 60 * 1000);
                        break;
                    default:
                    case "s":
                        t = (int)(num * 1000);
                        break;
                }
            }

            return t;
        }

        /// <summary>
        /// 发送的头信息
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// 发送的内容，
        /// </summary>
        public string[]? Contents { get; set; }

        /// <summary>
        /// 配置来源，上游来源，一般为空，如果是CollectTarget的Version，默认随机值，说明没有上游
        /// </summary>
        internal string Trace { get; set; } = Guid.NewGuid().ToString("n");

        public Dictionary<string, Dictionary<string, object>> Prepare { get; set; }

        /// <summary>
        /// 对返回内容的转换器
        /// </summary>
        public Dictionary<string, Dictionary<string, object>> Transform { get; set; }

        public string GetVersion()
        {
            return HashHelper.SHA1(SerializerHelper.ToPlainString(this));
        }
    }
}
