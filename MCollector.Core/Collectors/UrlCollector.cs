using MCollector.Core.Contracts;
using System.Diagnostics;

namespace MCollector.Core.Collectors
{
    //[Collector("url")]
    internal class UrlCollector: ICollector
    {
        IHttpClientFactory _httpClientFactory;

        public UrlCollector(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public virtual string Type => "url";

        private Dictionary<string, int> _conentHeaders = new Dictionary<string, int>(StringComparer.InvariantCultureIgnoreCase) {
            { "Content-Type", 0 }, {"Content-Encoding", 1} , {"Content-Language", 2} , {"Content-Location", 3 } , {"Content-MD5", 4 } , {"Content-Range", 5 }
        };
        public virtual async Task<CollectedData> Collect(CollectTarget target)
        {
            var client = _httpClientFactory.CreateClient("default");

            var msg = new HttpRequestMessage();
            msg.Method = HttpMethod.Get;
            msg.RequestUri = new Uri(target.Target, UriKind.Absolute);
            if (target.Contents?.Any() == true)
            {
                msg.Method = HttpMethod.Post;
                msg.Content = new StringContent(string.Join(Environment.NewLine, target.Contents));
                var headers = target.Headers?.Where(h => _conentHeaders.ContainsKey(h.Key));
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        switch (_conentHeaders[header.Key])
                        {
                            case 0:
                                msg.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(header.Value);
                                break;
                        }
                    }
                }
            }

            if(target.Headers?.Any() == true)
            {
                foreach(var header in target.Headers)
                {
                    if (_conentHeaders.ContainsKey(header.Key) == false)
                    {
                        msg.Headers.Add(header.Key, header.Value);
                    }
                }
            }

            var sw = new Stopwatch();
            sw.Start();

            var data = new CollectedData(target.Name, target);

            using var cts = new CancellationTokenSource();
            try
            {
                var isCompleted = false;
                cts.CancelAfter(Math.Min(target.GetInterval(), 6000));//默认6秒超时
                //Task.Run(async () =>
                //{
                //    await Task.Delay(Math.Min(target.Interval, 6000), cts.Token);
                //    if (!isCompleted)
                //        cts.Cancel();
                //});

                var res = await client.SendAsync(msg, cts.Token);
                isCompleted = true;

                data.IsSuccess = (int)res.StatusCode < 400;//400以上认为采集失败，但还是会继续处理body内容
                data.Headers = res.Headers.ToDictionary(h => h.Key, h => (object)string.Join(",", h.Value));
                //data.Code = (int)res.StatusCode;
                data.Content = await res.Content.ReadAsStringAsync();
                if (!data.IsSuccess && string.IsNullOrWhiteSpace(data.Content))
                {
                    data.Content = ((int)res.StatusCode).ToString();
                }
            }
            catch (Exception ex)
            {
                if((ex is TaskCanceledException cex) && cex.CancellationToken.IsCancellationRequested == true)
                {
                    data.Content = "请求超时";
                }
                else
                {
                    data.Content = ex.GetBaseException()?.Message;
                }

                data.IsSuccess = false;
            }

            return data;
        }
    }
}
