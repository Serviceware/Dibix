using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    /// <summary>
    /// Trace if a proxy was applied to the request (Trace source: System.Net)
    /// </summary>
    public sealed class TraceProxyHttpMessageHandler : DelegatingHandler
    {
        #region Fields
        private static readonly TraceSource ProxyTraceSource = new TraceSource("System.Net");
        #endregion

        #region Overrides
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            TraceProxy(request);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region Private Methods
        private static void TraceProxy(HttpRequestMessage request)
        {
            if (System.Net.WebRequest.DefaultWebProxy == null)
                return;

            bool isBypassed = System.Net.WebRequest.DefaultWebProxy.IsBypassed(request.RequestUri);
            ProxyTraceSource.TraceInformation($"[Proxy] IsBypassed {request.RequestUri}: {isBypassed}");
        }
        #endregion
    }
}