using Newtonsoft.Json;

namespace Dibix.Http.Client.OpenIdConnect
{
    public sealed class OpenIdConnectDiscoveryDocument
    {
        [JsonProperty("token_endpoint")]
        public string TokenEndpoint { get; set; }
    }
}