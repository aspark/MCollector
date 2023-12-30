using MCollector.Core.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MCollector.Core.Contracts
{
    public class CollectTarget
    {
        private Lazy<Func<int>> _fnInterval;
        private Lazy<Func<int>> _fnRetryInterval;

        public CollectTarget()
        {
            _fnInterval = new Lazy<Func<int>>(() => ParseInterval(Interval), false);
            _fnRetryInterval = new Lazy<Func<int>>(() => ParseInterval(!string.IsNullOrWhiteSpace(RetryInterval)? RetryInterval: Interval), false);
        }

        private string _name = string.Empty;

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
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// 采集器的配置
        /// </summary>
        public Dictionary<string, object> Args { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// 目标
        /// </summary>
        public string Target { get; set; } = string.Empty;

        /// <summary>
        /// 间隔时间（毫秒）
        /// </summary>
        public string Interval { get; set; } = "3s";

        /// <summary>
        /// 出错重试的，时间间隔
        /// </summary>
        public string RetryInterval { get; set; } = string.Empty;

        private Random _rand = new Random(DateTime.Now.Millisecond);
        public int GetInterval(bool isRetry = false)
        {
            return (isRetry ? _fnRetryInterval.Value : _fnInterval.Value)();
        }

        public Func<int> ParseInterval(string internval)
        {
            Func<int> fn = null;

            if (internval.StartsWith("rand", StringComparison.InvariantCultureIgnoreCase))
            {
                Regex ex = new Regex(@"\((?<s>[^,]+),?\s*(?<e>[^\s]+)?\)");
                var m = ex.Match(internval);
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

                    fn = new Func<int>(() => _rand.Next(start, end));
                }
            }
            else if (internval.StartsWith('['))
            {
                var arr = internval.Trim('[', ']').Split(',').Select(i => ParseTime(i.Trim())).ToArray();
                var i = 0;

                fn = new Func<int>(() => {
                    if (i >= arr.Length) i = 0;

                    return arr[i++ % arr.Length];
                });
            }
            else
            {
                var interval = ParseTime(internval);

                fn = new Func<int>(() => interval);
            }

            if (fn == null)
                fn = new Func<int>(() => 3000);

            return fn;
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
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 发送的内容，
        /// </summary>
        public string[]? Contents { get; set; }

        /// <summary>
        /// 配置来源，上游来源，一般为空，如果是CollectTarget的Version，默认随机值，说明没有上游
        /// </summary>
        internal string Trace { get; set; } = Guid.NewGuid().ToString("n");

        public Dictionary<string, Dictionary<string, object>> Prepare { get; set; } = new Dictionary<string, Dictionary<string, object>>();

        /// <summary>
        /// 对返回内容的转换器
        /// </summary>
        public TransferConfig Transform { get; set; } = new TransferConfig();

        /// <summary>
        /// 自定义配置，可用于扩展的实现中，如：Export
        /// </summary>
        public ExtrasConfig Extras { get; set; } = new ExtrasConfig();

        public string GetVersion()
        {
            return HashHelper.SHA1(SerializerHelper.ToPlainString(this));
        }
    }

    public class TransferConfig: Dictionary<string, Dictionary<string, object>>
    {

    }

    public class ExtrasConfig: Dictionary<string, dynamic>
    {
        /// <summary>
        /// 通过path获取对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool TryGetCustomConfig<T>(string path, out T? config) where T: class, new()
        {
            config = null;

            if(this.Count == 0)
                return false;

            return SerializerHelper.TryCreateFrom(this, path, out config);
        }
    }
}
