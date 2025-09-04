using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;

namespace Dibix.Http.Server.AspNet
{
    public sealed class HttpRequestMessageDescriptor : IHttpRequestDescriptor
    {
        internal HttpRequestMessage RequestMessage { get; }

        public HttpRequestMessageDescriptor(HttpRequestMessage request)
        {
            RequestMessage = request;
        }

        public string GetPath() => RequestMessage.RequestUri!.AbsolutePath;

        public Stream GetBody() => RequestMessage.Content?.ReadAsStreamAsync().GetAwaiter().GetResult();

        public string GetBodyMediaType() => RequestMessage.Content?.Headers.ContentType?.MediaType;

        public string GetBodyFileName() => RequestMessage.Content?.Headers.ContentDisposition?.FileName;

        public IEnumerable<string> GetHeaderValues(string name) => RequestMessage.Headers.TryGetValues(name, out IEnumerable<string> values) ? values : [];

        public IEnumerable<string> GetAcceptLanguageValues() => RequestMessage.Headers.AcceptLanguage.Select(x => x.Value);

        public ClaimsPrincipal GetUser()
        {
            return RequestMessage.GetUser();
        }

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
    }
}