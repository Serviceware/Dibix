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
        private const int MinimumSqlThrowErrorNumber = 50000;

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
                if (action.OmitResult || result == null)
                    return request.CreateResponse(HttpStatusCode.NoContent);

                if (result is HttpResponse httpResponse)
                    return httpResponse.CreateResponse(request);

                return result;
            }
            catch (DatabaseAccessException exception) when (exception.InnerException is SqlException sqlException)
            {
                // Sample:
                // THROW 50403, N'Web shop not licensed', 1
                // Returns HttpStatusCode.Forbidden [403]
                if (TryGetHttpStatusCode(sqlException.Number, out HttpStatusCode statusCode))
                    return request.CreateResponse(statusCode);

                throw;
            }
        }

        private static bool TryGetHttpStatusCode(int errorNumber, out HttpStatusCode statusCode)
        {
            int statusCodeNumber = errorNumber - MinimumSqlThrowErrorNumber;
            if (Enum.IsDefined(typeof(HttpStatusCode), statusCodeNumber))
            {
                statusCode = (HttpStatusCode)statusCodeNumber;
                return true;
            }

            statusCode = default;
            return false;
        }
    }
}
