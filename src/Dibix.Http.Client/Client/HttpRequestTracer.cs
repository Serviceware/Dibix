using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public class HttpRequestTracer
    {
        public bool MaskSensitiveContent { get; } = true;

        public HttpRequestTracer() { }
        public HttpRequestTracer(bool maskSensitiveContent)
        {
            this.MaskSensitiveContent = maskSensitiveContent;
        }

        internal async Task TraceRequestAsync(HttpRequestMessage requestMessage)
        {
            HttpRequestTrace requestTrace = CreateRequestTrace();
            requestMessage.SetHttpRequestTrace(requestTrace);
            await this.TraceRequestAsync(requestMessage, requestTrace).ConfigureAwait(false);
        }

        internal async Task TraceResponseAsync(HttpResponseMessage responseMessage, TimeSpan duration)
        {
            HttpRequestTrace requestTrace = responseMessage.RequestMessage.GetHttpRequestTrace();
            CompleteLastRequest(requestTrace, duration);
            await this.TraceResponseAsync(responseMessage, requestTrace);
        }

        protected virtual Task TraceRequestAsync(HttpRequestMessage requestMessage, HttpRequestTrace requestTrace) => Task.CompletedTask;

        protected virtual Task TraceResponseAsync(HttpResponseMessage responseMessage, HttpRequestTrace requestTrace) => Task.CompletedTask;

        private protected virtual HttpRequestTrace CreateRequestTrace() => new HttpRequestTrace();

        private static void CompleteLastRequest(HttpRequestTrace requestTrace, TimeSpan duration)
        {
            requestTrace.Duration = duration;
        }
    }

    public class HttpRequestTracer<T> : HttpRequestTracer where T : HttpRequestTrace, new()
    {
        protected sealed override Task TraceRequestAsync(HttpRequestMessage requestMessage, HttpRequestTrace requestTrace) => this.TraceRequestAsync(requestMessage, (T)requestTrace);

        protected sealed override Task TraceResponseAsync(HttpResponseMessage responseMessage, HttpRequestTrace requestTrace) => this.TraceResponseAsync(responseMessage, (T)requestTrace);

        private protected sealed override HttpRequestTrace CreateRequestTrace() => new T();

        protected virtual Task TraceRequestAsync(HttpRequestMessage requestMessage, T requestTrace) => Task.CompletedTask;

        protected virtual Task TraceResponseAsync(HttpResponseMessage responseMessage, T requestTrace) => Task.CompletedTask;
    }
}