using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public static class HttpActionInvoker
    {
        public static async Task<object> Invoke
        (
            HttpActionDefinition action
          , HttpRequestMessage request
          , IDictionary<string, object> arguments
          , IHttpParameterResolutionMethod parameterResolver
          , Func<Task<object>> executor
          , IParameterDependencyResolver parameterDependencyResolver
        )
        {
            try
            {
                parameterResolver.PrepareParameters(request, arguments, parameterDependencyResolver);
                object result = await executor().ConfigureAwait(false);
                object formattedResult = HttpResponseFormatter.Format(action, result, request);
                return formattedResult;
            }
            catch (DatabaseAccessException exception) when (exception.InnerException is SqlException sqlException)
            {
                // Sample:
                // THROW 404017, N'Feature not configured', 1
                // 404017 => 404 17 => HttpStatusCode.NotFound (ResultCode: 17) - ResultCode can be a more specific application/feature error code
                // 
                // HTTP/1.1 404 NotFound
                // X-Result-Code: 17
                if (TryParseHttpError(exception, sqlException, request, out HttpRequestExecutionException httpException))
                    throw httpException;

                throw;
            }
        }

        private static bool TryParseHttpError(Exception innerException, SqlException sqlException, HttpRequestMessage request, out HttpRequestExecutionException exception)
        {
            if (!HttpErrorResponseParser.TryParseErrorResponse(sqlException.Number, out int statusCode, out int errorCode, out bool isClientError))
            {
                exception = null;
                return false;
            }

            HttpResponseMessage errorResponse = request.CreateResponse((HttpStatusCode)statusCode);

            if (isClientError)
            {
                if (errorCode != 0)
                    errorResponse.Headers.Add(KnownHeaders.ClientErrorCodeHeaderName, errorCode.ToString());

                errorResponse.Headers.Add(KnownHeaders.ClientErrorDescriptionHeaderName, sqlException.Message);
                errorResponse.Content = new StringContent($"\"{sqlException.Message}\"", Encoding.UTF8, "application/json");
            }

            exception = new HttpRequestExecutionException(errorResponse, isClientError, innerException);
            return true;
        }
    }
}