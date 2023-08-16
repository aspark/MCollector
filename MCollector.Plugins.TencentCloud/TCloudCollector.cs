

using MCollector.Core.Common;
using MCollector.Core.Contracts;
using TencentCloud.Common;
using TencentCloud.Tke.V20180525;
using TencentCloud.Tke.V20180525.Models;

namespace MCollector.Plugins.TencentCloud
{
    public class TCloudCollector : ICollector
    {
        public string Type => "tcloud";

        public async Task<CollectedData> Collect(CollectTarget target)
        {
            var args = SerializerHelper.CreateFrom<TCloudCollectorArgs>(target.Args) ?? new TCloudCollectorArgs();
            var cred = new Credential
            {
                SecretId = args.SecretID,
                SecretKey = args.SecretKey
            };
            var client = new TkeClient(null, args.Region);
            var res = await client.DescribeEKSContainerInstances(new DescribeEKSContainerInstancesRequest
            {

            });

            //https://cloud.tencent.com/document/product/457/61662
            if (res.EksCis?.Length > 0)
            {
                //res.EksCis[0].Containers
            }

            throw new NotImplementedException();
        }
    }

    internal class TCloudCollectorArgs 
    {
        public string SecretID { get; set; }

        public string SecretKey { get; set; }

        public string Region { get; set; } = "ap-guangzhou";
    }

}