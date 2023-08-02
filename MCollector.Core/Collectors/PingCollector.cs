using MCollector.Core.Contracts;
using System.Net.NetworkInformation;

namespace MCollector.Core.Collectors
{
    //[Collector("ping")]
    internal class PingCollector : ICollector
    {
        public string Type => "ping";

        public async Task<CollectedData> Collect(CollectTarget target)
        {
            var data = new CollectedData(target.Name, target);

            try
            {
                using (Ping ping = new Ping())
                {
                    var reply = await ping.SendPingAsync(target.Target, Math.Min(target.Interval, 300));//300ms timeout
                    if (reply.Status == IPStatus.Success)
                    {
                        data.IsSuccess = true;
                    }
                    else
                    {
                        data.IsSuccess = false;
                        data.Content = reply.Status.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                data.IsSuccess = false;
                data.Content = ex.GetBaseException()?.Message;
            }

            return data;
        }
    }
}
