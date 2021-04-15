using Newtonsoft.Json;

namespace Dibix.Http.Client.OpenIdConnect
{
    public sealed class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }
}