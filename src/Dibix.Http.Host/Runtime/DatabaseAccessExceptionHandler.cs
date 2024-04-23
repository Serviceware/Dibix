using System;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host
{
    internal sealed class DatabaseAccessExceptionHandler : IExceptionHandler
    {
        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) => new(TryHandle(httpContext, exception));

        private static bool TryHandle(HttpContext httpContext, Exception exception)
        {
            if (exception is not DatabaseAccessException databaseAccessException) 
                return false;

            if (!SqlHttpStatusCodeParser.TryParse(databaseAccessException, out HttpRequestExecutionException httpRequestExecutionException)) 
                return false;

            httpRequestExecutionException.AppendToResponse(httpContext.Response);
            return true;
        }
    }
}