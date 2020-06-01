using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http
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
          , IParameterDependencyResolver parameterDependencyResolver)
        {
            try
            {
                parameterResolver.PrepareParameters(request, arguments, parameterDependencyResolver);
                object result = await executor().ConfigureAwait(false);
                if (result is HttpResponse httpResponse)
                    return httpResponse.CreateResponse(request);

                return result;
            }
            catch (DatabaseAccessException exception) when (exception.InnerException is SqlException sqlException)
            {
                // Sample:
                // THROW 404017, N'Feature not configured', 1
                // 404017 => 404 17 => HttpStatusCode.NotFound (ResultCode: 17) - ResultCode can be a more specific application/feature error code
                // 
                // HTTP/1.1 404 NotFound
                // X-Result-Code: 17
                if (TryParseValidationResponse(sqlException.Number, sqlException.Message, request, out HttpResponseMessage response))
                    return response;

                throw;
            }
        }

        private static bool TryParseValidationResponse(int errorNumber, string errorDescription, HttpRequestMessage request, out HttpResponseMessage response)
        {
            if (!HttpErrorResponseParser.TryParseErrorResponse(ref errorDescription, errorNumber, out int statusCode, out int errorCode))
            {
                response = null;
                return false;
            }

            response = request.CreateResponse((HttpStatusCode)statusCode);

            if (errorCode != 0)
                response.Headers.Add(HttpErrorResponseParser.ClientErrorCodeHeaderName, errorCode.ToString());

            if (!String.IsNullOrEmpty(errorDescription))
            {
                response.Headers.Add(HttpErrorResponseParser.ClientErrorDescriptionHeaderName, errorDescription);
                response.Content = new StringContent(errorDescription);
            }

            return true;
        }
    }
}