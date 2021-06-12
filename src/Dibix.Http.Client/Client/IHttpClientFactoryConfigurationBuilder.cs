using System;

namespace Dibix.Http.Client
{
    public interface IHttpClientFactoryConfigurationBuilder
    {
        Uri BaseAddress { get; set; }
        HttpRequestTracer HttpRequestTracer { get; set; }
        bool FollowRedirectsForGetRequests { get; set; }

        void AddUserAgent(Action<IHttpUserAgentSelectorExpression> selector);
    }
}