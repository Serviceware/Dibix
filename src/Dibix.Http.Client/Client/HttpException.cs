using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Dibix.Http.Client
{
    public class HttpException : Exception
    {
        public HttpStatusCode StatusCode => Response.StatusCode;
        public HttpRequestMessage Request { get; }
        public string RequestContentText { get; }
        public HttpResponseMessage Response { get; }
        public string ResponseContentText { get; }

        private HttpException(HttpRequestMessage request, string requestContentText, HttpResponseMessage response, string responseContentText) : this(request, requestContentText, response, responseContentText, CreateMessage(response)) { }
        private protected HttpException(HttpRequestMessage request, string requestContentText, HttpResponseMessage response, string responseContentText, string message) : base(message)
        {
            Request = request;
            RequestContentText = requestContentText;
            Response = response;
            ResponseContentText = responseContentText;
        }

        public static async Task<HttpException> Create(HttpRequestMessage request, HttpResponseMessage response)
        {
            string requestContentText = null;
            if (request.Content != null)
                requestContentText = await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            string responseContentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            try
            {
                (int? errorCode, string errorMessage) = ReadProblemDetails(response.Content, responseContentText);
                if (errorCode == null)
                    return new HttpException(request, requestContentText, response, responseContentText);

                return new HttpValidationException(request, requestContentText, response, responseContentText, errorCode.Value, errorMessage);
            }
            finally
            {
                response.Content.Dispose();
            }
        }

        private static (int? code, string errorMessage) ReadProblemDetails(HttpContent content, string responseContentText)
        {
            const string contentType = "application/problem+json";
            if (!String.Equals(content.Headers.ContentType?.MediaType, contentType, StringComparison.OrdinalIgnoreCase))
                return (null, null);

            JObject problemDetails = JObject.Parse(responseContentText);
            int? code = (int?)problemDetails.Property("code")?.Value;
            if (code == null)
                return (null, null);

            string errorMessage = (string)problemDetails.Property("detail")?.Value;
            return (code, errorMessage);
        }

        private protected static string CreateMessage(HttpResponseMessage response)
        {
            Guard.IsNotNull(response, nameof(response));
            string message = $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}).";
            return message;
        }
    }
}