using System;
using System.Threading.Tasks;

namespace Dibix.Http.Client.OpenIdConnect
{
    public interface IOpenIdConnectAuthenticator
    {
        Task<TokenResponse> Authenticate(Uri authority, string userName, string password, string clientId);
    }
}