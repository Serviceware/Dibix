using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    public static class HttpMessageFormatter
    {
        #region Fields
        private static readonly Func<HttpHeaders, IEnumerable<KeyValuePair<string, string>>> GetHeaderStringsAccessor = CompileGetHeaderStrings();
        #endregion

        #region Public Methods
        public static string Format(string formattedRequestText, string formattedResponseText, int? maxLength = null)
        {
            string formattedRequestTextTrimmed = formattedRequestText;
            string formattedResponseTextTrimmed = formattedResponseText;

            if (maxLength.HasValue)
            {
                formattedRequestTextTrimmed = formattedRequestTextTrimmed.TrimIfNecessary(maxLength.Value);
                formattedResponseTextTrimmed = formattedResponseTextTrimmed.TrimIfNecessary(maxLength.Value);
            }

            return $@"Request
-------
{formattedRequestTextTrimmed}

Response
--------
{formattedResponseTextTrimmed}";
        }
        #endregion

        #region Internal Methods
        internal static async Task<string> Format(HttpRequestMessage requestMessage, bool maskSensitiveData)
        {
            string requestMessageContent = null;
            if (requestMessage.Content != null)
                requestMessageContent = await requestMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

            return Format(requestMessage, requestMessageContent, maskSensitiveData);
        }
        internal static string Format(HttpRequestMessage requestMessage, string requestContentText, bool maskSensitiveData)
        {
            Guard.IsNotNull(requestMessage, nameof(requestMessage));

            StringBuilder sb = new StringBuilder($"{requestMessage.Method} {requestMessage.RequestUri} HTTP/{requestMessage.Version}");

            if (requestMessage.Headers.Any())
            {
                sb.AppendLine()
                  .Append(Format(requestMessage.Headers));
            }

            if (Equals(requestMessage.Content?.Headers.Any(), true))
            {
                sb.AppendLine()
                  .Append(Format(requestMessage.Content.Headers));
            }

            if (requestContentText != null)
            {
                string secureRequestContentText = requestContentText;
                if (maskSensitiveData)
                    secureRequestContentText = Regex.Replace(secureRequestContentText, "password=[^&]+", "password=*****");

                sb.AppendLine()
                  .AppendLine()
                  .Append(secureRequestContentText);
            }

            string formattedRequest = sb.ToString();
            return formattedRequest;
        }

        internal static async Task<string> Format(HttpResponseMessage responseMessage)
        {
            string responseContentText = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Format(responseMessage, responseContentText);
        }
        internal static string Format(HttpResponseMessage responseMessage, string responseContentText)
        {
            Guard.IsNotNull(responseMessage, nameof(responseMessage));

            StringBuilder sb = new StringBuilder($"HTTP/{responseMessage.Version} {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase}");

            if (responseMessage.Headers.Any())
            {
                sb.AppendLine()
                  .Append(Format(responseMessage.Headers));
            }

            if (responseMessage.Content.Headers.Any())
            {
                sb.AppendLine()
                  .Append(Format(responseMessage.Content.Headers));
            }

            if (responseContentText.Length > 0)
            {
                sb.AppendLine()
                  .AppendLine()
                  .Append(responseContentText);
            }

            string formattedResponse = sb.ToString();
            return formattedResponse;
        }
        #endregion

        #region Private Methods
        private static string Format(HttpHeaders headers)
        {
            IDictionary<string, string> headerStrings = GetHeaderStringsAccessor(headers).ToDictionary(x => x.Key, x => x.Value);
            return String.Join(Environment.NewLine, headers.Select(x => $"{GetHeaderString(headers, headerStrings, x.Key)}"));
        }

        // The only different to the base implementation is, that it doesn't print sensitive authorization header information
        private static string GetHeaderString(IEnumerable headers, IDictionary<string, string> headerStrings, string headerName)
        {
            if (headers is HttpRequestHeaders requestHeaders && headerName == nameof(HttpRequestHeaders.Authorization))
                return $"{nameof(HttpRequestHeaders.Authorization)}: {requestHeaders.Authorization.Scheme} {TrimAuthorizationValue(requestHeaders.Authorization.Parameter)}";

            return $"{headerName}: {headerStrings[headerName]}";
        }

        private static string TrimAuthorizationValue(string value) => value.Length < 5 ? value : $"{value.Substring(0, 5)}...";

        private static string TrimIfNecessary(this string text, int maxLength) => text?.Length > maxLength ? $"{text.Substring(0, maxLength)}..." : text;

        private static Func<HttpHeaders, IEnumerable<KeyValuePair<string, string>>> CompileGetHeaderStrings()
        {
            ParameterExpression headers = Expression.Parameter(typeof(HttpHeaders), "headers");

            Expression call = Expression.Call(headers, "GetHeaderStrings", new Type[0]);
            Expression<Func<HttpHeaders, IEnumerable<KeyValuePair<string, string>>>> lambda = Expression.Lambda<Func<HttpHeaders, IEnumerable<KeyValuePair<string, string>>>>(call, headers);
            Func<HttpHeaders, IEnumerable<KeyValuePair<string, string>>> compiled = lambda.Compile();
            return compiled;
        }
        #endregion
    }
}