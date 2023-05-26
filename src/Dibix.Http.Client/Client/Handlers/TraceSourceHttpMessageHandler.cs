using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public sealed class TraceSourceHttpMessageHandler : DelegatingHandler
    {
        #region Fields
        private readonly DibixHttpClientTraceSource _traceSource;
        private readonly Random _random;
        #endregion

        #region Constructor
        public TraceSourceHttpMessageHandler()
        {
            _traceSource = new DibixHttpClientTraceSource("Dibix.Http.Client");
            _random = new Random();
        }
        #endregion

        #region Overrides
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int requestId = _random.Next(minValue: 1000, maxValue: 10000);
            await WriteRequestToTraceSource(request, requestId).ConfigureAwait(false);

            HttpResponseMessage responseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            await WriteResponseToTraceSource(responseMessage, requestId).ConfigureAwait(false);

            return responseMessage;
        }
        #endregion

        #region Private Methods
        private async Task WriteRequestToTraceSource(HttpRequestMessage request, int requestId)
        {
            string GetRequestText(string requestContentText) => request.GetFormattedText(requestContentText, _traceSource.MaxBodyLength, _traceSource.MaskSensitiveBody);
            await WriteToTraceSource("request", requestId, request.Content, GetRequestText).ConfigureAwait(false);
        }

        private async Task WriteResponseToTraceSource(HttpResponseMessage response, int requestId)
        {
            string GetResponseText(string responseContentText) => response.GetFormattedText(responseContentText, _traceSource.MaxBodyLength, _traceSource.MaskSensitiveBody);
            await WriteToTraceSource("response", requestId, response.Content, GetResponseText).ConfigureAwait(false);
        }

        private async Task WriteToTraceSource(string kind, int requestId, HttpContent content, Func<string, string> textProvider)
        {
            if (!_traceSource.Switch.ShouldTrace(TraceEventType.Information))
                return;

            string contentText = await GetHttpContentText(content).ConfigureAwait(false);
            string text = textProvider(contentText);
            string header = $"HTTP {kind} [{requestId}]";
            string line = $@"
{new string('=', header.Length)}
";
            string message = $@"{header}
{line}
{text}";
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