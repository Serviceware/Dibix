using System;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host
{
    // Map sql error codes to http status codes globally
    // This is needed if a DatabaseAccessException is thrown outside HttpActionInvoker. For example, within the http host extension.
    internal sealed class DatabaseAccessExceptionHandler : HttpRequestExecutionExceptionHandler, IExceptionHandler
    {
        public DatabaseAccessExceptionHandler(IProblemDetailsService problemDetailsService) : base(problemDetailsService) { }

        public override async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not DatabaseAccessException databaseAccessException)
                return false;

            if (SqlHttpStatusCodeParser.TryParse(databaseAccessException, out HttpRequestExecutionException httpRequestExecutionException))
                return await base.TryHandleAsync(httpContext, httpRequestExecutionException, cancellationToken).ConfigureAwait(false);

            return false;
        }
    }
}