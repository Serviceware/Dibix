using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    /// <summary>
    /// - Get request message diagnostics (usually it's disposed after the request; required opt-in to avoid large in memory allocations)
    /// - Get response message diagnostics
    /// - Measure request duration
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
            Guard.IsNotNull(tracer, nameof(tracer));
            this._tracer = tracer;
        }
        #endregion

        #region Overrides
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await this.TraceRequest(request).ConfigureAwait(false);

            try
            {
                this.StartTracking();
                HttpResponseMessage responseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                this.FinishTracking();

                await this.TraceResponse(responseMessage).ConfigureAwait(false);

                return responseMessage;
            }
            finally
            {
                this.FinishTracking();
            }
        }
        #endregion

        #region Private Methods
        private async Task TraceRequest(HttpRequestMessage request)
        {
            string formattedRequest = await HttpMessageFormatter.Format(request, this._tracer.MaskSensitiveData).ConfigureAwait(false);
            this._tracer.TraceRequest(request, formattedRequest);
        }

        private async Task TraceResponse(HttpResponseMessage response)
        {
            string formattedResponse = await HttpMessageFormatter.Format(response).ConfigureAwait(false);
            this._tracer.TraceResponse(response, formattedResponse, this._requestDurationTracker.Elapsed);
        }

        private void StartTracking() => this._requestDurationTracker.Restart();

        private void FinishTracking() => this._requestDurationTracker.Stop();
        #endregion
    }
}