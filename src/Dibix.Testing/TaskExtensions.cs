using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Testing
{
    internal static class TaskExtensions
    {
        public static async Task<TResult> Retry<TResult>(this Func<Task<TResult>> taskMethod, Func<TResult, bool> condition, int millisecondsDelay, int millisecondsTimeout)
        {
            using (CancellationTokenSource cts = new CancellationTokenSource())
            {
                cts.CancelAfter(millisecondsTimeout);

                for (int i = 1;; i++)
                {
                    TResult result = await taskMethod().ConfigureAwait(false);
                    if (condition(result))
                        return result;

                    try
                    {
                        await Task.Delay(millisecondsDelay, cts.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        if (cts.IsCancellationRequested)
                            throw new InvalidOperationException($"Awaiting asynchronous response timed out after {TimeSpan.FromMilliseconds(millisecondsTimeout)} and {i} attempts");

                        throw;
                    }
                }
            }
        }
    }
}