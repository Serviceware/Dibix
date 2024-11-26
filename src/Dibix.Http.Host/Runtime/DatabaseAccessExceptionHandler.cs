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
    internal sealed class DatabaseAccessExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;

        public DatabaseAccessExceptionHandler(IProblemDetailsService problemDetailsService)
        {
            _problemDetailsService = problemDetailsService;
        }

        public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) => TryHandleWithProblemDetails(httpContext, exception);
        
        // TODO: Instead of writing problem details manually, use the StatusCodeSelector introduced in .NET 9
        // See: https://www.milanjovanovic.tech/blog/problem-details-for-aspnetcore-apis#handling-specific-exceptions-status-codes
        private async ValueTask<bool> TryHandleWithProblemDetails(HttpContext httpContext, Exception exception)
        {
            bool result = TryHandle(httpContext, exception, out HttpRequestExecutionException? httpRequestExecutionException);
            if (!result)
                return false;

            ProblemDetailsContext problemDetailsContext = new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = httpRequestExecutionException
            };
            return await _problemDetailsService.TryWriteAsync(problemDetailsContext).ConfigureAwait(false);
        }

        private static bool TryHandle(HttpContext httpContext, Exception exception, out HttpRequestExecutionException? httpRequestExecutionException)
        {
            if (exception is not DatabaseAccessException databaseAccessException)
            {
                httpRequestExecutionException = null;
                return false;
            }

            if (!SqlHttpStatusCodeParser.TryParse(databaseAccessException, out httpRequestExecutionException))
                return false;

            httpContext.Response.StatusCode = (int)httpRequestExecutionException.StatusCode;

            // For compatibility reasons
            // TODO: Remove, once problem details are stabilized
            httpRequestExecutionException.AppendToResponse(httpContext.Response);

            // Note: The ExceptionHandlerMiddleware will log the exception even if the handler returns true
            // See: https://github.com/dotnet/aspnetcore/issues/54554
            // If client exceptions should not be logged, we can do the following:
            // - Implement a custom IExceptionHandler
            // - Use LogError with a custom log category (i.E. the name of the handler, something like 'HttpStatusCodeClientExceptionHandler')
            // - Set this category's log level to 'Critical' by default, to avoid logging client exceptions
            return true;
        }
    }
}