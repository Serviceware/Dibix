using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Client
{
    /// <summary>
    /// If the configured timeout on the HttpClient for a HTTP request is reached, a TaskCanceledException is thrown.
    /// There is no stable way to determine, if the actual request got canceled, or a timeout occurred.
    /// This handler throws a more specific exception, if timeouts occur.
    /// There is also a way to handle this using .NET 5.0: https://github.com/dotnet/runtime/pull/2281
    /// Note: The Timeout property on the HttpClient still has to be configured manually to a larger value, than the timeout on this handler.
    /// </summary>
    internal sealed class TimeoutHttpMessageHandler : DelegatingHandler
    {
        #region Properties
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(100d); // HttpClient.defaultTimeout
        #endregion

        #region Overrides
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (CancellationTokenSource cts = GetCancellationTokenSource(cancellationToken))
            {
                try
                {
                    cts?.Token.ThrowIfCancellationRequested();
                    return await base.SendAsync(request, cts?.Token ?? cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    throw new TimeoutException($"Timeout for HTTP request has been reached: {Timeout}");
                }
            }
        }
        #endregion

        #region Private Methods
        private CancellationTokenSource GetCancellationTokenSource(CancellationToken cancellationToken)
        {
            if (Timeout == System.Threading.Timeout.InfiniteTimeSpan)
            {
                // No need to create a CTS if there's no timeout
                return null;
            }

            CancellationTokenSource timeoutTokenSource = new CancellationTokenSource();
            CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);
            timeoutTokenSource.CancelAfter(Timeout);
            return linkedTokenSource;
        }
        #endregion
    }
}