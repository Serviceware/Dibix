using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    /// <summary>
    /// Automatically follow redirects for GET requests
    /// </summary>
    public sealed class FollowRedirectHttpMessageHandler : DelegatingHandler
    {
        #region Fields
        private bool _allowAutoRedirectResolved;
        #endregion

        #region Overrides
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.ConfigureRedirect(request);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
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

        private static HttpClientHandler FindClientHandler(System.Net.Http.HttpMessageHandler handler)
        {
            switch (handler)
            {
                case HttpClientHandler clientHandler: return clientHandler;
                case DelegatingHandler delegatingHandler: return FindClientHandler(delegatingHandler.InnerHandler);
                default: return null;
            }
        }
        #endregion
    }
}