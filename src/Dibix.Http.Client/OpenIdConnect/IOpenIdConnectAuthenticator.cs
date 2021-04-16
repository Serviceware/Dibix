using System;
using System.Threading.Tasks;

namespace Dibix.Http.Client.OpenIdConnect
{
    public interface IOpenIdConnectAuthenticator
    {
        Task<TokenResponse> Login(Uri authority, string clientId, string userName, string password);
        Task<TokenResponse> RefreshToken(Uri authority, string clientId, string refreshToken);
    }
}