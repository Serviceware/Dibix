using System.Security.Claims;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host.Runtime
{
    internal sealed class RequestIdentityProvider : IRequestIdentityProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public RequestIdentityProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ClaimsPrincipal? GetIdentity() => _httpContextAccessor.HttpContext?.User;
    }
}
