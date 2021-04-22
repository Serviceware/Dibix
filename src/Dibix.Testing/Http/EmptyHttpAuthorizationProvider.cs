using Dibix.Http.Client;

namespace Dibix.Testing.Http
{
    public sealed class EmptyHttpAuthorizationProvider : IHttpAuthorizationProvider
    {
        public string GetValue(string headerName) => null;
    }
}