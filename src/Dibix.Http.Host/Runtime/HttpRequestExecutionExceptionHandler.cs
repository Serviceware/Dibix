﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host
{
    // Map custom HTTP status codes to response
    internal class HttpRequestExecutionExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;

        public HttpRequestExecutionExceptionHandler(IProblemDetailsService problemDetailsService)
        {
            _problemDetailsService = problemDetailsService;
        }

        public virtual ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken) => TryHandleWithProblemDetails(httpContext, exception);

        protected static bool HandleHttpRequestExecutionException(HttpContext httpContext, HttpRequestExecutionException httpRequestExecutionException)
        {
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

        // TODO: Instead of writing problem details manually, use the StatusCodeSelector introduced in .NET 9
        // See: https://www.milanjovanovic.tech/blog/problem-details-for-aspnetcore-apis#handling-specific-exceptions-status-codes
        private async ValueTask<bool> TryHandleWithProblemDetails(HttpContext httpContext, Exception exception)
        {
            bool result = TryHandle(httpContext, exception);
            if (!result)
                return false;

            ProblemDetailsContext problemDetailsContext = new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception
            };
            return await _problemDetailsService.TryWriteAsync(problemDetailsContext).ConfigureAwait(false);
        }

        private static bool TryHandle(HttpContext httpContext, Exception exception)
        {
            if (exception is not HttpRequestExecutionException httpRequestExecutionException)
                return false;

            return HandleHttpRequestExecutionException(httpContext, httpRequestExecutionException);
        }
    }
}