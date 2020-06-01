using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace Dibix.Http
{
    public static class HttpActionInvoker
    {
        private const string ClientErrorCodeHeaderName = "X-Error-Code";
        private const string ClientErrorDescriptionHeaderName = "X-Error-Description";
        private static readonly int[] ClientErrorHttpStatuses =
        {
            (int)HttpStatusCode.BadRequest                 // Client syntax error (malformed request)
          , (int)HttpStatusCode.NotFound                   // Feature not available/configured
          , (int)HttpStatusCode.Conflict                   // Locks (might resolve by retry)
          , 422 //(int)HttpStatusCode.UnprocessableEntity  // Client semantic error (schema error/validation)
        };
        private static readonly int[] ServerErrorHttpStatuses =
        {
            (int)HttpStatusCode.GatewayTimeout  // External service did not respond in time
        };

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
            response = null;
            if (errorNumber == 0)
                return false;

            const int expectedDigitLength = 6;
            int length = (int)Math.Log10(Math.Abs(errorNumber)) + 1;
            if (length != expectedDigitLength)
                return false;

            int httpStatusCode = errorNumber / 1000;
            int errorCode = errorNumber % 1000;

            bool isServerError = ServerErrorHttpStatuses.Contains(httpStatusCode) && errorCode == 0;
            bool isClientError = ClientErrorHttpStatuses.Contains(httpStatusCode);

            if (!isServerError && !isClientError)
                return false;

            response = request.CreateResponse((HttpStatusCode)httpStatusCode);

            if (isClientError)
            {
                if (errorCode != 0)
                    response.Headers.Add(ClientErrorCodeHeaderName, errorCode.ToString());
                
                response.Headers.Add(ClientErrorDescriptionHeaderName, errorDescription);
                response.Content = new StringContent(errorDescription);
            }

            return true;
        }
    }
}
