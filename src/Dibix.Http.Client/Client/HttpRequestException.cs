using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public sealed class HttpRequestException : Exception
    {
        public HttpStatusCode StatusCode => this.Response.StatusCode;
        public HttpRequestMessage Request => this.Response.RequestMessage;
        public HttpResponseMessage Response { get; }
        public HttpContentHeaders ResponseContentHeaders { get; }
        public string ResponseContentText { get; }

        private HttpRequestException(HttpResponseMessage response, HttpContentHeaders responseContentHeaders, string responseContentText) : base(CreateMessage(response))
        {
            this.Response = response;
            this.ResponseContentHeaders = responseContentHeaders;
            this.ResponseContentText = responseContentText;
        }

        internal static async Task<HttpRequestException> Create(HttpResponseMessage response)
        {
            HttpContentHeaders contentHeaders = null;
            string contentText = null;
 
            if (response.Content != null)
            {
                contentHeaders = response.Content.Headers;
                contentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                response.Content.Dispose();
            }

            return new HttpRequestException(response, contentHeaders, contentText);
        }

        private static string CreateMessage(HttpResponseMessage response)
        {
            Guard.IsNotNull(response, nameof(response));
            return $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).";
        }
    }
}