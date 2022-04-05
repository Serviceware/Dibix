using System.Security.Claims;

namespace Dibix.Http.Server
{
    public interface IRequestIdentityProvider
    {
        ClaimsPrincipal GetIdentity();
    }
}
