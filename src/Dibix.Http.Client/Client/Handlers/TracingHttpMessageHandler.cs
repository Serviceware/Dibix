using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    /// <summary>
    /// - Capture request/response message including formatted content text
    /// - Measure request duration
    /// - Optionally delegate captured request/response to custom tracer which can act as an interceptor
    /// </summary>
    public sealed class TracingHttpMessageHandler : DelegatingHandler
    {
        #region Fields
        private readonly Stopwatch _requestDurationTracker = new Stopwatch();
        private readonly HttpRequestTracer _tracer;
        #endregion

        #region Constructor
        public TracingHttpMessageHandler(HttpRequestTracer tracer)
        {
            _tracer = tracer;
        }
        #endregion

        #region Overrides
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await TraceRequest(request).ConfigureAwait(false);

            try
            {
                StartTracking();
                HttpResponseMessage responseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                FinishTracking();

                await TraceResponse(responseMessage).ConfigureAwait(false);

                return responseMessage;
            }
            finally
            {
                FinishTracking();
            }
        }
        #endregion

        #region Private Methods
        private async Task TraceRequest(HttpRequestMessage request)
        {
            await _tracer.TraceRequestAsync(request).ConfigureAwait(false);
        }

        private async Task TraceResponse(HttpResponseMessage response)
        {
            await _tracer.TraceResponseAsync(response, _requestDurationTracker.Elapsed).ConfigureAwait(false);
        }

        private void StartTracking() => _requestDurationTracker.Restart();

        private void FinishTracking() => _requestDurationTracker.Stop();
        #endregion
    }
}