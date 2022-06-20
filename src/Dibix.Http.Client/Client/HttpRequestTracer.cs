using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public class HttpRequestTracer
    {
        public bool MaskSensitiveContent { get; } = true;
        public HttpRequestTrace LastRequest { get; private set; }

        public HttpRequestTracer() { }
        public HttpRequestTracer(bool maskSensitiveContent)
        {
            this.MaskSensitiveContent = maskSensitiveContent;
        }

        internal async Task TraceRequestMessageAsync(HttpRequestMessage requestMessage)
        {
            await this.CollectLastRequest(requestMessage).ConfigureAwait(false);
            await this.TraceRequestAsync(requestMessage).ConfigureAwait(false);
        }

        internal async Task TraceResponseMessageAsync(HttpResponseMessage responseMessage, TimeSpan duration)
        {
            await CompleteLastRequest(responseMessage, duration).ConfigureAwait(false);
            await this.TraceResponseAsync(responseMessage, duration);
        }

        protected virtual Task TraceRequestAsync(HttpRequestMessage requestMessage) => Task.CompletedTask;

        protected virtual Task TraceResponseAsync(HttpResponseMessage responseMessage, TimeSpan duration) => Task.CompletedTask;

        protected virtual bool ShouldBufferRequestContent(HttpRequestMessage requestMessage) => false;

        protected virtual bool ShouldBufferResponseContent(HttpRequestMessage requestMessage) => false;

        private async Task CollectLastRequest(HttpRequestMessage requestMessage)
        {
            string formattedRequestText = await this.GetFormattedRequestContent(requestMessage).ConfigureAwait(false);
            this.LastRequest = new HttpRequestTrace(requestMessage, formattedRequestText);
        }

        private async Task CompleteLastRequest(HttpResponseMessage responseMessage, TimeSpan duration)
        {
            if (this.LastRequest == null)
                throw new InvalidOperationException("Request not initialized");

            string formattedResponseText = await this.GetFormattedResponseContent(responseMessage).ConfigureAwait(false);
            this.LastRequest.ResponseMessage = responseMessage;
            this.LastRequest.FormattedResponseText = formattedResponseText;
            this.LastRequest.Duration = duration;

            // Since non successful status code will throw an exception,
            // it is now safe to restore the request content, that was not previously captured
            HttpRequestMessage requestMessage = responseMessage.RequestMessage;
            if (!this.ShouldBufferRequestContent(requestMessage) && !responseMessage.IsSuccessStatusCode)
            {
                string requestContentText = await GetRequestContentTextAsync(requestMessage, bufferRequestContent: true).ConfigureAwait(false);
                this.LastRequest.FormattedRequestText = this.FormatRequest(requestMessage, requestContentText);
            }
        }

        private async Task<string> GetFormattedRequestContent(HttpRequestMessage requestMessage)
        {
            bool bufferRequestContent = this.ShouldBufferRequestContent(requestMessage);
            string requestContentText = await GetRequestContentTextAsync(requestMessage, bufferRequestContent).ConfigureAwait(false);
            string formattedRequestText = this.FormatRequest(requestMessage, requestContentText);
            return formattedRequestText;
        }

        private async Task<string> GetFormattedResponseContent(HttpResponseMessage responseMessage)
        {
            bool bufferResponseContent = this.ShouldBufferResponseContent(responseMessage.RequestMessage) || !responseMessage.IsSuccessStatusCode;
            string responseContentTest = await GetResponseContentTextAsync(responseMessage, bufferResponseContent).ConfigureAwait(false);
            string formattedResponseText = HttpMessageFormatter.Format(responseMessage, responseContentTest);
            return formattedResponseText;
        }

        private string FormatRequest(HttpRequestMessage requestMessage, string requestContentText) => HttpMessageFormatter.Format(requestMessage, requestContentText, this.MaskSensitiveContent);

        private static async Task<string> GetRequestContentTextAsync(HttpRequestMessage requestMessage, bool bufferRequestContent)
        {
            if (requestMessage.Content == null)
                return null;

            if (!bufferRequestContent)
                return "<Request content unavailable>";

            return await requestMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        private static async Task<string> GetResponseContentTextAsync(HttpResponseMessage responseMessage, bool bufferResponseContent)
        {
            if (responseMessage.Content == null)
                return null;

            if (!bufferResponseContent)
                return "<Response content unavailable>";

            return await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
        }
    }
}