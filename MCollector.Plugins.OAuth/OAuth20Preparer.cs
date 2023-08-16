using IdentityModel.Client;
using MCollector.Core.Contracts;

namespace MCollector.Plugins.OAuth
{
    public class OAuth20Preparer : PreparerBase<OAuthPreparerArgs>
    {
        IHttpClientFactory _httpClientFactory;

        public OAuth20Preparer(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public override string Name => "oauth20";

        protected override async Task Process(CollectTarget target, OAuthPreparerArgs args)
        {
            var request = new ClientCredentialsTokenRequest
            {
                Address = args.Address,
                ClientId = "client",
                ClientSecret = "secret"
            };

            var client = _httpClientFactory.CreateClient();

            var res = await client.RequestClientCredentialsTokenAsync(request);

            if(res.IsError == false)
                target.Headers["Authorization"] = "Bearer " + res.AccessToken;
            else
            {
                Console.WriteLine(res.Error);
            }
        }
    }

    public class OAuthPreparerArgs
    {
        public string Address { get; set; }
        public string ClientId { get; set; }

        public string ClientSecret { get; set; }
    }
}