using System;
using Dibix.Http.Server;

namespace Dibix.Http.Host
{
    public sealed class EndpointDefinition
    {
        public string Method { get; }
        public Uri Url { get; }
        public HttpActionDefinition Definition { get; }

        public EndpointDefinition(string method, Uri url, HttpActionDefinition definition)
        {
            Method = method;
            Url = url;
            Definition = definition;
        }
    }
}