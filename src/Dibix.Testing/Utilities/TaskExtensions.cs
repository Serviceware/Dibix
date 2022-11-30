using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Testing
{
    internal static class TaskExtensions
    {
        public static async Task<TResult> Retry<TResult>(this Func<CancellationToken, Task<TResult>> taskMethod, Func<TResult, bool> condition, int millisecondsDelay, int millisecondsTimeout, CancellationToken cancellationToken = default)
        {
            using (CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                cts.CancelAfter(millisecondsTimeout);

                for (int i = 1;; i++)
                {
                    try
                    {
                        TResult result = await taskMethod(cts.Token).ConfigureAwait(false);
                        if (condition(result))
                            return result;
                        
                        await Task.Delay(millisecondsDelay, cts.Token).ConfigureAwait(false);
                    }
                    catch (Exception exception) when (IsCancellationException(exception, cts.Token))
                    {
                        throw new InvalidOperationException($"Awaiting asynchronous response timed out after {TimeSpan.FromMilliseconds(millisecondsTimeout)} and {i} attempts");
                    }
                }
            }
        }
        
        private static bool IsCancellationException(Exception exception, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
                return false;

            switch (exception)
            {
                case DatabaseAccessException databaseAccessException: return IsCancellationException(databaseAccessException.InnerException, cancellationToken);
                case SqlException sqlException: return IsPossiblySqlCommandCancellation(sqlException);
                case TaskCanceledException _: return true;
                case OperationCanceledException _: return true;
                default: return false;
            }

        }

        private static bool IsPossiblySqlCommandCancellation(SqlException sqlException)
        {
            // A severe error occurred on the current command.  The results, if any, should be discarded.
            // Operation cancelled by user.
            return sqlException.Class == 11 && sqlException.Number == 0;
        }
    }
}