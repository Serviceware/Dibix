using Newtonsoft.Json;

namespace Dibix.Http.Client.OpenIdConnect
{
    public sealed class TokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }
}