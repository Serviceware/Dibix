using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public sealed class HttpRequestMessageDescriptor : IHttpRequestDescriptor
    {
        internal HttpRequestMessage RequestMessage { get; }
        
        public HttpRequestMessageDescriptor(HttpRequestMessage request)
        {
            RequestMessage = request;
        }

        public async Task<Stream> GetBody() => RequestMessage.Content != null ? await RequestMessage.Content.ReadAsStreamAsync().ConfigureAwait(false) : null;
        
        public IEnumerable<string> GetHeaderValues(string name) => RequestMessage.Headers.TryGetValues(name, out IEnumerable<string> values) ? values : Enumerable.Empty<string>();

        public IEnumerable<string> GetAcceptLanguageValues() => RequestMessage.Headers.AcceptLanguage.Select(x => x.Value);
        
        public ClaimsPrincipal GetUser() => throw new NotSupportedException();

        public HttpActionDefinition GetActionDefinition() => throw new NotSupportedException();

        public string GetRemoteAddress()
        {
#if NETFRAMEWORK
            System.Web.HttpContextBase context = RequestMessage.Properties["MS_HttpContext"] as System.Web.HttpContextBase;
            return context?.Request.UserHostAddress;
#else
            throw new NotSupportedException();
#endif
        }

        public string GetRemoteName()
        {
#if NETFRAMEWORK
            System.Web.HttpContextBase context = RequestMessage.Properties["MS_HttpContext"] as System.Web.HttpContextBase;
            return context?.Request.UserHostName;
#else
            throw new NotSupportedException();
#endif
        }

        public string GetBearerToken() => throw new NotSupportedException();

        public DateTime? GetBearerTokenExpiresAt() => throw new NotSupportedException();

        public object CreateResponse(HttpStatusCode statusCode) => RequestMessage.CreateResponse(statusCode);

        public object CreateFileResponse(string contentType, byte[] data, string fileName, bool cache)
        {
            HttpResponseMessage response = RequestMessage.CreateResponse();
            response.Content = new ByteArrayContent(data);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("inline") { FileName = fileName };

            if (cache)
            {
                DateTime now = DateTime.Now;
                TimeSpan year = now.AddYears(1) - now;
                response.Headers.CacheControl = new CacheControlHeaderValue { MaxAge = year };
            }

            return response;
        }
    }
}