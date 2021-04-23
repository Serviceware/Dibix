using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public class HttpRequestTracer
    {
        public bool MaskSensitiveData { get; set; } = true;
        public HttpRequestTrace LastRequest { get; private set; }

        internal async Task TraceRequestAsync(HttpRequestMessage requestMessage)
        {
            string requestContentText = await this.GetRequestContentTextAsync(requestMessage).ConfigureAwait(false);
            string formattedRequestText = this.FormatRequest(requestMessage, requestContentText);
            await this.TraceRequestAsync(requestMessage, formattedRequestText).ConfigureAwait(false);
        }

        internal async Task TraceResponseAsync(HttpResponseMessage responseMessage, TimeSpan duration)
        {
            string responseContentTest = await this.GetResponseContentTextAsync(responseMessage).ConfigureAwait(false);
            string formattedResponseText = this.FormatResponse(responseMessage, responseContentTest);
            await this.TraceResponseAsync(responseMessage, formattedResponseText, duration).ConfigureAwait(false);
        }

        protected virtual Task TraceRequestAsync(HttpRequestMessage requestMessage, string formattedRequestText)
        {
            this.LastRequest = new HttpRequestTrace(requestMessage, formattedRequestText);
            return Task.CompletedTask;
        }

        protected virtual Task TraceResponseAsync(HttpResponseMessage responseMessage, string formattedResponseText, TimeSpan duration)
        {
            if (this.LastRequest == null)
                throw new InvalidOperationException("Request not initialized");

            this.LastRequest.ResponseMessage = responseMessage;
            this.LastRequest.FormattedResponseText = formattedResponseText;
            this.LastRequest.Duration = duration;
            
            return Task.CompletedTask;
        }

        protected virtual string FormatRequest(HttpRequestMessage requestMessage, string requestContentText) => HttpMessageFormatter.Format(requestMessage, requestContentText, this.MaskSensitiveData);

        protected virtual string FormatResponse(HttpResponseMessage responseMessage, string responseContentTest) => HttpMessageFormatter.Format(responseMessage, responseContentTest);

        protected virtual async Task<string> GetRequestContentTextAsync(HttpRequestMessage requestMessage) => requestMessage.Content != null ? await requestMessage.Content.ReadAsStringAsync().ConfigureAwait(false) : null;
        
        protected virtual Task<string> GetResponseContentTextAsync(HttpResponseMessage responseMessage) => responseMessage.Content.ReadAsStringAsync();
    }
}