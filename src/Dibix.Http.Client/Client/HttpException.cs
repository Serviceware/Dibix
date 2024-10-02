using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public class HttpException : Exception
    {
        public HttpStatusCode StatusCode => this.Response.StatusCode;
        public HttpRequestMessage Request { get; }
        public string RequestContentText { get; }
        public HttpResponseMessage Response { get; }
        public string ResponseContentText { get; }

        private HttpException(HttpRequestMessage request, string requestContentText, HttpResponseMessage response, string responseContentText) : this(request, requestContentText, response, responseContentText, CreateMessage(response)) { }
        private protected HttpException(HttpRequestMessage request, string requestContentText, HttpResponseMessage response, string responseContentText, string message) : base(message)
        {
            this.Request = request;
            this.RequestContentText = requestContentText;
            this.Response = response;
            this.ResponseContentText = responseContentText;
        }

        public static async Task<HttpException> Create(HttpRequestMessage request, HttpResponseMessage response)
        {
            string requestContentText = null;
            if (request.Content != null)
                requestContentText = await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            string responseContentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            response.Content.Dispose();

            if (!response.Headers.TryGetSingleValue(KnownHeaders.ClientErrorCodeHeaderName, out string value)) 
                return new HttpException(request, requestContentText, response, responseContentText);

            if (!Int32.TryParse(value, out int errorCode))
                throw new InvalidOperationException($"Could not parse error code from header '{KnownHeaders.ClientErrorCodeHeaderName}': {value}");

            response.Headers.TryGetSingleValue(KnownHeaders.ClientErrorDescriptionHeaderName, out string errorMessage);
            return new HttpValidationException(request, requestContentText, response, responseContentText, errorCode, errorMessage);
        }

        private protected static string CreateMessage(HttpResponseMessage response)
        {
            Guard.IsNotNull(response, nameof(response));
            string message = $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).";
            return message;
        }
    }
}