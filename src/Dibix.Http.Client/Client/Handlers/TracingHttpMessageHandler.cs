using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    /// <summary>
    /// - Capture request/response message including formatted content text
    /// - Measure request duration
    /// - Write captured request/response to trace source
    /// - Optionally delegate captured request/response to custom tracer which can act as an interceptor
    /// </summary>
    public sealed class TracingHttpMessageHandler : DelegatingHandler
    {
        #region Fields
        private readonly Stopwatch _requestDurationTracker = new Stopwatch();
        private readonly HttpRequestTracer _tracer;
        private readonly DibixHttpClientTraceSource _traceSource;
        #endregion

        #region Constructor
        public TracingHttpMessageHandler(HttpRequestTracer tracer)
        {
            _tracer = tracer;
            _traceSource = new DibixHttpClientTraceSource("Dibix.Http.Client");
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
            await WriteRequestToTraceSource(request).ConfigureAwait(false);
            
            if (_tracer != null)
                await _tracer.TraceRequestAsync(request).ConfigureAwait(false);
        }

        private async Task TraceResponse(HttpResponseMessage response)
        {
            await WriteResponseToTraceSource(response).ConfigureAwait(false);
            
            if (_tracer != null) 
                await _tracer.TraceResponseAsync(response, _requestDurationTracker.Elapsed).ConfigureAwait(false);
        }

        private void StartTracking() => _requestDurationTracker.Restart();

        private void FinishTracking() => _requestDurationTracker.Stop();

        private async Task WriteRequestToTraceSource(HttpRequestMessage request)
        {
            if (!_traceSource.Switch.ShouldTrace(TraceEventType.Information))
                return;

            string requestContentText = await GetHttpContentText(request.Content).ConfigureAwait(false);
            string requestText = request.GetFormattedText(requestContentText, _traceSource.MaxBodyLength, _traceSource.MaskSensitiveBody);
            string message = $@"HTTP request
-
{requestText}";
            _traceSource.TraceInformation(message);
        }

        private async Task WriteResponseToTraceSource(HttpResponseMessage response)
        {
            if (!_traceSource.Switch.ShouldTrace(TraceEventType.Information))
                return;

            string responseContentText = await GetHttpContentText(response.Content).ConfigureAwait(false);
            string responseText = response.GetFormattedText(responseContentText, _traceSource.MaxBodyLength, _traceSource.MaskSensitiveBody);
            string message = $@"HTTP response
-
{responseText}";
            _traceSource.TraceInformation(message);
        }

        private async Task<string> GetHttpContentText(HttpContent content)
        {
            if (content == null)
                return null;

            bool loadIntoBuffer = false;
            if (_traceSource.AlwaysBufferBody)
            {
                loadIntoBuffer = true;
            }
            else if (content.Headers.ContentLength.HasValue)
            {
                if (content.Headers.ContentLength > _traceSource.MaxBodySize)
                    return $"<Body is {content.Headers.ContentLength} bytes, which exceeds the configured limit of {_traceSource.MaxBodySize}. Increase it using the trace option MaxBodySize=\"<ValueInBytes>\".>";

                loadIntoBuffer = true;
            }

            if (loadIntoBuffer)
                return await content.ReadAsStringAsync().ConfigureAwait(false);
            
            return "<Body is not loaded into buffer. Configure the trace option AlwaysBufferBody=\"True\" to collect it.>";
        }
        #endregion

        #region Nested Types
        private sealed class DibixHttpClientTraceSource : TraceSource
        {
            private const string AlwaysBufferBodyPropertyName  = "AlwaysBufferBody";
            private const string MaskSensitiveBodyPropertyName = "MaskSensitiveBody";
            private const string MaxBodySizePropertyName       = "MaxBodySize";
            private const string MaxBodyLengthPropertyName     = "MaxBodyLength";
            private const int DefaultMaxBodySize               = 32 * 1024; // 32 KB

            public bool AlwaysBufferBody => Boolean.TryParse(Attributes[AlwaysBufferBodyPropertyName], out bool alwaysBufferBody) && alwaysBufferBody;
            public bool MaskSensitiveBody => !Boolean.TryParse(Attributes[MaskSensitiveBodyPropertyName], out bool maskSensitiveBodyValue) || maskSensitiveBodyValue;
            public int MaxBodySize
            {
                get
                {
                    int maxBodySize = DefaultMaxBodySize;
                    if (Int32.TryParse(Attributes[MaxBodySizePropertyName], out int maxBodySizeValue))
                        maxBodySize = maxBodySizeValue;

                    return maxBodySize;
                }
            }
            public int? MaxBodyLength
            {
                get
                {
                    int? maxBodyLength = null;
                    if (Int32.TryParse(Attributes[MaxBodyLengthPropertyName], out int maxBodyLengthValue))
                        maxBodyLength = maxBodyLengthValue;

                    return maxBodyLength;
                }
            }

            public DibixHttpClientTraceSource(string name) : base(name) { }
            public DibixHttpClientTraceSource(string name, SourceLevels defaultLevel) : base(name, defaultLevel) { }

            protected override string[] GetSupportedAttributes() => new[]
            {
                AlwaysBufferBodyPropertyName,
                MaskSensitiveBodyPropertyName,
                MaxBodySizePropertyName,
                MaxBodyLengthPropertyName
            };
        }
        #endregion
    }
}