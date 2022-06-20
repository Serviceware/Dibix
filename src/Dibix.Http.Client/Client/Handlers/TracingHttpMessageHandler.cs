using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    /// <summary>
    /// - Capture request/response message including formatted content text
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
        private Task TraceRequest(HttpRequestMessage request) => this._tracer.TraceRequestMessageAsync(request);

        private Task TraceResponse(HttpResponseMessage response) => this._tracer.TraceResponseMessageAsync(response, this._requestDurationTracker.Elapsed);

        private void StartTracking() => this._requestDurationTracker.Restart();

        private void FinishTracking() => this._requestDurationTracker.Stop();
        #endregion
    }
}