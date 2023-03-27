using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Worker.Abstractions
{
    public static class ExceptionUtility
    {
        public static bool IsCancellationException(Exception? exception, CancellationToken cancellationToken)
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
            return sqlException is { Class: 11, Number: 0 };
        }
    }
}