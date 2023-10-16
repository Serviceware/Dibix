using System;
using Dibix.Http.Server;

namespace Dibix.Http.Host
{
    public sealed class EndpointDefinition
    {
        public string Method { get; }
        public Uri Url { get; }
        public HttpActionDefinition ActionDefinition { get; }

        public EndpointDefinition(string method, Uri url, HttpActionDefinition actionDefinition)
        {
            Method = method;
            Url = url;
            ActionDefinition = actionDefinition;
        }
    }
}