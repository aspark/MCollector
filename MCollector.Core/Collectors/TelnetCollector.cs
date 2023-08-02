using MCollector.Core.Contracts;
using System.Net.Sockets;

namespace MCollector.Core.Abstracts
{
    //[Collector("")]
    internal class TelnetCollector : ICollector
    {
        public string Type => "telnet";

        public async Task<CollectedData> Collect(CollectTarget target)
        {
            var data = new CollectedData(target.Name, target);

            var endpoint = target.Target.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
            if (endpoint.Length < 2)
            {
                data.IsSuccess = false;
                data.Content = "IP格式不对，应为：127.0.0.1:8080";
            }
            else
            {
                using (TcpClient client = new TcpClient())//todo 使用telnet组件。。。
                {
                    try
                    {
                        await client.ConnectAsync(endpoint[0], int.Parse(endpoint[1]));
                        if (client.Connected)
                        {
                            data.IsSuccess = true;
                            data.Content = string.Empty;
                            client.Close();
                        }
                        else
                            data.IsSuccess = false;
                    }
                    catch (Exception ex)
                    {
                        data.IsSuccess = false;
                        data.Content = ex.GetBaseException()?.Message;
                    }
                }
            }

            return data;
        }
    }
}
