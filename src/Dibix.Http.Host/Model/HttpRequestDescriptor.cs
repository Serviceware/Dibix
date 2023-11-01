using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host
{
    internal sealed class HttpRequestDescriptor : IHttpRequestDescriptor
    {
        private readonly HttpRequest _request;

        public HttpRequestDescriptor(HttpRequest request)
        {
            _request = request;
        }

        public Task<Stream> GetBody() => Task.FromResult(_request.Body);

        public IEnumerable<string> GetHeaderValues(string name) => _request.Headers[name];
        
        public IEnumerable<string> GetAcceptLanguageValues() => _request.GetTypedHeaders().AcceptLanguage.Select(x => x.Value.Value).Where(x => x != null).Select(x => x!);
        
        public ClaimsPrincipal GetUser() => _request.HttpContext.User;
    }
}