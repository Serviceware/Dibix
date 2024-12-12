using System;

namespace Dibix.Http.Server
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