using System.Net.Http;
using System.Security.Claims;

namespace Dibix.Http.Server.AspNet
{
    public static class HttpRequestMessageExtensions
    {
        private const string UserKey = "DBX_USER";
        
        public static void SetUser(this HttpRequestMessage request, ClaimsPrincipal principal) => request.Properties[UserKey] = principal;

        internal static ClaimsPrincipal GetUser(this HttpRequestMessage request) => request.Properties.TryGetValue(UserKey, out object value) ? value as ClaimsPrincipal : new ClaimsPrincipal(new ClaimsIdentity());
    }
}