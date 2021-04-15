using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    /// <summary>
    /// This factory applies the following comfort features to <see cref="System.Net.Http.HttpClient"/>:
    /// - Ensure success status code by throwing without disposing the response message content
    /// - Trace if a proxy was applied to the request (Trace source: System.Net)
    /// - Automatically follow redirects for GET requests
    ///
    /// The following features apply, if a tracer was supplied
    /// - Get request message diagnostics (usually it's disposed after the request; required opt-in to avoid large in memory allocations)
    /// - Get response message diagnostics
    /// - Measure request duration
    /// </summary>
    public class HttpClientFactory : IHttpClientFactory
    {
        #region Fields
        private readonly IHttpRequestTracer _httpRequestTracer;
        #endregion

        #region Constructor
        public HttpClientFactory() { }
        public HttpClientFactory(IHttpRequestTracer httpRequestTracer)
        {
            this._httpRequestTracer = httpRequestTracer;
        }
        #endregion

        #region IHttpClientFactory Members
        public HttpClient Create()
        {
            HttpMessageHandler messageHandler = new HttpMessageHandler(this._httpRequestTracer);
            HttpClient client = new HttpClient(messageHandler);
            this.ConfigureClient(client);
            return client;
        }
        #endregion

        #region Protected Methods
        protected virtual void ConfigureClient(HttpClient client) { }
        #endregion

        #region Nested Types
        private sealed class HttpMessageHandler : DelegatingHandler
        {
            #region Fields
            private static readonly Func<HttpHeaders, IEnumerable<KeyValuePair<string, string>>> GetHeaderStringsAccessor = CompileGetHeaderStrings();
            private static readonly TraceSource ProxyTraceSource = new TraceSource("System.Net");
            private readonly Stopwatch _requestDurationTracker = new Stopwatch();
            private readonly IHttpRequestTracer _tracer;
            private bool _allowAutoRedirectResolved;
            #endregion

            #region Constructor
            public HttpMessageHandler(IHttpRequestTracer tracer) : base(new HttpClientHandler())
            {
                this._tracer = tracer;
            }
            #endregion

            #region Overrides
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                this.ConfigureRedirect(request);
                await this.TraceRequest(request).ConfigureAwait(false);

                try
                {
                    TraceProxy(request);

                    this.StartTracking();
                    HttpResponseMessage responseMessage = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    this.FinishTracking();

                    await this.TraceResponse(responseMessage).ConfigureAwait(false);

                    // We are not using the builtin EnsureSuccessStatusCode method on HttpResponseMessage,
                    // since it disposes the response content, before we can capture it for diagnostics.
                    //responseMessage.EnsureSuccessStatusCode();
                    if (!responseMessage.IsSuccessStatusCode)
                        throw await HttpRequestException.Create(responseMessage).ConfigureAwait(false);

                    return responseMessage;
                }
                finally
                {
                    this.FinishTracking();
                }
            }
            #endregion

            #region Private Methods
            /// <summary>
            /// TLDR: We only follow redirects automatically for GET requests.
            /// ---
            /// In HTTP 1.1, there actually is a status code (307) which indicates that the request should be repeated using the same method and post data.
            ///
            /// As others have said, there is a potential for misuse here which may be why many frameworks stick to 301 and 302 in their abstractions. However, with proper understanding and responsible usage, you should be able to accomplish what you're looking for.
            /// 
            /// Note that according to the W3.org spec, when the METHOD is not HEAD or GET, user agents should prompt the user before re-executing the request at the new location. You should also provide a note and a fallback mechanism for the user in case old user agents aren't sure what to do with a 307. 
            /// </summary>
            /// <see cref="https://softwareengineering.stackexchange.com/questions/99894/why-doesnt-http-have-post-redirect"/>
            private void ConfigureRedirect(HttpRequestMessage request)
            {
                if (this._allowAutoRedirectResolved)
                    return;

                HttpClientHandler httpClientHandler = FindClientHandler(this.InnerHandler);
                if (httpClientHandler == null) 
                    return;

                httpClientHandler.AllowAutoRedirect = request.Method == HttpMethod.Get;
                this._allowAutoRedirectResolved = true; // see: System.Net.Http.HttpClientHandler.CheckDisposedOrStarted
            }

            private async Task TraceRequest(HttpRequestMessage request)
            {
                if (this._tracer == null)
                    return;

                string requestMessageContent = null;
                if (request.Content != null)
                    requestMessageContent = this._tracer.CollectRequestContent ? await request.Content.ReadAsStringAsync().ConfigureAwait(false) : "<BODY NOT AVAILABLE>";

                string requestDump = Dump(request, requestMessageContent);
                this._tracer.TraceRequest(request, requestDump);
            }

            private async Task TraceResponse(HttpResponseMessage response)
            {
                if (this._tracer == null)
                    return;

                string responseDump = await Dump(response).ConfigureAwait(false);
                this._tracer.TraceResponse(response, responseDump, this._requestDurationTracker.Elapsed);
            }

            private void StartTracking() => this._requestDurationTracker.Restart();

            private void FinishTracking() => this._requestDurationTracker.Stop();

            private static void TraceProxy(HttpRequestMessage request)
            {
                if (System.Net.WebRequest.DefaultWebProxy == null)
                    return;

                bool isBypassed = System.Net.WebRequest.DefaultWebProxy.IsBypassed(request.RequestUri);
                ProxyTraceSource.TraceInformation($"[Proxy] IsBypassed {request.RequestUri}: {isBypassed}");
            }

            private static HttpClientHandler FindClientHandler(System.Net.Http.HttpMessageHandler handler)
            {
                switch (handler)
                {
                    case HttpClientHandler clientHandler: return clientHandler;
                    case DelegatingHandler delegatingHandler: return FindClientHandler(delegatingHandler.InnerHandler);
                    default: return null;
                }
            }

            private static string Dump(HttpRequestMessage request, string requestContent)
            {
                if (request == null)
                    return null;

                StringBuilder sb = new StringBuilder($"{request.Method} {request.RequestUri} HTTP/{request.Version}").AppendLine();
                sb.Append(Dump(request.Headers));

                if (request.Content != null)
                    sb.Append(request.Content.Headers);

                if (requestContent != null)
                {
                    string secureRequestContent = Regex.Replace(requestContent, "password=[^&]+", "password=*****");

                    sb.AppendLine()
                      .Append(secureRequestContent);
                }

                return sb.ToString();
            }

            private static string Dump(HttpRequestHeaders headers)
            {
                IDictionary<string, string> headerStrings = GetHeaderStringsAccessor(headers).ToDictionary(x => x.Key, x => x.Value);
                return String.Join(String.Empty, headers.Select(x => $"{GetHeaderString(headers, headerStrings, x.Key)}{Environment.NewLine}"));
            }

            // The only different to the base implementation is, that it doesn't print sensible authorization header information
            private static string GetHeaderString(HttpRequestHeaders headers, IDictionary<string, string> headerStrings, string headerName)
            {
                if (headerName == nameof(HttpRequestHeaders.Authorization))
                    return $"{nameof(HttpRequestHeaders.Authorization)}: {headers.Authorization.Scheme} {TrimAuthorizationValue(headers.Authorization.Parameter)}";

                return $"{headerName}: {headerStrings[headerName]}";
            }

            private static string TrimAuthorizationValue(string value) => value.Length < 5 ? value : $"{value.Substring(0, 5)}...";

            private static async Task<string> Dump(HttpResponseMessage response)
            {
                if (response == null)
                    return null;

                StringBuilder sb = new StringBuilder($"HTTP/{response.Version} {(int)response.StatusCode} {response.ReasonPhrase}").AppendLine();
                sb.Append(response.Headers);

                if (response.Content != null)
                {
                    string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                    sb.Append(response.Content.Headers)
                      .AppendLine()
                      .Append(responseContent);
                }

                return sb.ToString();
            }

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
        #endregion
    }
}