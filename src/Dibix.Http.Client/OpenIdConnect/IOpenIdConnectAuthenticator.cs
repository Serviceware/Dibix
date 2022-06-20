using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http.Client.OpenIdConnect
{
    public interface IOpenIdConnectAuthenticator
    {
        Task<TokenResponse> Login(Uri authority, string clientId, string userName, string password);
        Task<TokenResponse> Login(Uri authority, string clientId, string userName, string password, Action<HttpRequestMessage> requestFormatter);
        Task<TokenResponse> Login(Uri authority, string clientId, string clientSecret);
        Task<TokenResponse> Login(Uri authority, string clientId, string clientSecret, Action<HttpRequestMessage> requestFormatter);
        Task<TokenResponse> RefreshToken(Uri authority, string clientId, string refreshToken);
        Task<TokenResponse> RefreshToken(Uri authority, string clientId, string refreshToken, Action<HttpRequestMessage> requestFormatter);
    }
}