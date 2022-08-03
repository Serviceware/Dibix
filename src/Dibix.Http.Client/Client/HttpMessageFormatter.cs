using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

namespace Dibix.Http.Client
{
    internal static class HttpMessageFormatter
    {
        #region Public Methods
        public static string Format(HttpRequestMessage requestMessage, string requestContentText, bool maskSensitiveData, int? maxContentLength)
        {
            Guard.IsNotNull(requestMessage, nameof(requestMessage));

            StringBuilder sb = new StringBuilder($"{requestMessage.Method} {requestMessage.RequestUri} HTTP/{requestMessage.Version}");

            if (requestMessage.Headers.Any())
            {
                sb.AppendLine()
                  .Append(Format(requestMessage.Headers, maskSensitiveData));
            }

            if (Equals(requestMessage.Content?.Headers.Any(), true))
            {
                sb.AppendLine()
                  .Append(Format(requestMessage.Content.Headers, maskSensitiveData));
            }

            if (requestContentText != null)
            {
                string normalizedRequestContentText = requestContentText;
                if (maskSensitiveData)
                    normalizedRequestContentText = Regex.Replace(normalizedRequestContentText, "password=[^&]+", "password=*****");

                if (maxContentLength.HasValue)
                    normalizedRequestContentText = normalizedRequestContentText.TrimIfNecessary(maxContentLength.Value);

                sb.AppendLine()
                  .AppendLine()
                  .Append(normalizedRequestContentText);
            }

            string formattedRequest = sb.ToString();
            return formattedRequest;
        }

        public static string Format(HttpResponseMessage responseMessage, string responseContentText, bool maskSensitiveData, int? maxContentLength)
        {
            Guard.IsNotNull(responseMessage, nameof(responseMessage));

            StringBuilder sb = new StringBuilder($"HTTP/{responseMessage.Version} {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");

            if (responseMessage.Headers.Any())
            {
                sb.AppendLine()
                  .Append(Format(responseMessage.Headers, maskSensitiveData));
            }

            if (responseMessage.Content.Headers.Any())
            {
                sb.AppendLine()
                  .Append(Format(responseMessage.Content.Headers, maskSensitiveData));
            }

            if (responseContentText.Length > 0)
            {
                string normalizedResponseContentText = responseContentText;
                if (maxContentLength.HasValue) 
                    normalizedResponseContentText = normalizedResponseContentText.TrimIfNecessary(maxContentLength.Value);

                sb.AppendLine()
                  .AppendLine()
                  .Append(normalizedResponseContentText);
            }

            string formattedResponse = sb.ToString();
            return formattedResponse;
        }
        #endregion

        #region Private Methods
        private static string Format(HttpHeaders headers, bool maskSensitiveData)
        {
            string formattedHeaders = headers.ToString().TrimEnd();
            if (!maskSensitiveData)
                return formattedHeaders;

            string normalizedFormattedHeaders = Regex.Replace(formattedHeaders, @"(Authorization: [^ ]+ .{5})[^\s]+", "$1...");
            return normalizedFormattedHeaders;
        }

        private static string TrimIfNecessary(this string text, int maxLength) => text?.Length > maxLength ? $"{text.Substring(0, maxLength)}..." : text;
        #endregion
    }
}