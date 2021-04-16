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
    internal static class HttpMessageFormatter
    {
        #region Fields
        private static readonly Func<HttpHeaders, IEnumerable<KeyValuePair<string, string>>> GetHeaderStringsAccessor = CompileGetHeaderStrings();
        #endregion

        #region Public Methods
        public static async Task<string> Format(HttpRequestMessage request, bool maskSensitiveData)
        {
            string requestMessageContent = null;
            if (request.Content != null)
                requestMessageContent = await request.Content.ReadAsStringAsync().ConfigureAwait(false);

            return Format(request, requestMessageContent, maskSensitiveData);
        }
        public static string Format(HttpRequestMessage request, string requestContentText, bool maskSensitiveData)
        {
            Guard.IsNotNull(request, nameof(request));

            StringBuilder sb = new StringBuilder($"{request.Method} {request.RequestUri} HTTP/{request.Version}");

            if (request.Headers.Any())
            {
                sb.AppendLine()
                  .Append(Format(request.Headers));
            }

            if (Equals(request.Content?.Headers.Any(), true))
            {
                sb.AppendLine()
                  .Append(Format(request.Content.Headers));
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

        public static async Task<string> Format(HttpResponseMessage response)
        {
            string responseContentText = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return Format(response, responseContentText);
        }
        public static string Format(HttpResponseMessage response, string responseContentText)
        {
            Guard.IsNotNull(response, nameof(response));

            StringBuilder sb = new StringBuilder($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}");

            if (response.Headers.Any())
            {
                sb.AppendLine()
                  .Append(Format(response.Headers));
            }

            if (response.Content.Headers.Any())
            {
                sb.AppendLine()
                  .Append(Format(response.Content.Headers));
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