using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public sealed class HttpException : Exception
    {
        public HttpStatusCode StatusCode => this.Response.StatusCode;
        public HttpRequestMessage Request { get; }
        public string RequestContentText { get; }
        public HttpResponseMessage Response { get; }
        public string ResponseContentText { get; }

        private HttpException(HttpRequestMessage request, string requestContentText, HttpResponseMessage response, string responseContentText) : base(CreateMessage(response))
        {
            this.Request = request;
            this.RequestContentText = requestContentText;
            this.Response = response;
            this.ResponseContentText = responseContentText;
        }

        internal static async Task<HttpException> Create(HttpRequestMessage request, HttpResponseMessage response)
        {
            string requestContentText = null;
            if (request.Content != null) 
                requestContentText = await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            string responseContentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            response.Content.Dispose();

            return new HttpException(request, requestContentText, response, responseContentText);
        }

        private static string CreateMessage(HttpResponseMessage response)
        {
            Guard.IsNotNull(response, nameof(response));
            return $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).";
        }
    }
}