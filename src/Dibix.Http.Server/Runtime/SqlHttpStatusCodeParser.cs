using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Text.RegularExpressions;

namespace Dibix.Http.Server
{
    public static class SqlHttpStatusCodeParser
    {
        public static bool TryParse(DatabaseAccessException databaseAccessException, out HttpRequestExecutionException httpException)
        {
            return TryParse(databaseAccessException, action: null, arguments: new Dictionary<string, object>(), out httpException);
        }
        public static bool TryParse(DatabaseAccessException databaseAccessException, HttpActionDefinition action, IDictionary<string, object> arguments, out HttpRequestExecutionException httpException)
        {
            return TryParse(databaseAccessException, databaseAccessException.InnerException as SqlException, action, arguments, out httpException);
        }
        private  static bool TryParse(DatabaseAccessException originalException, SqlException rootException, HttpActionDefinition action, IDictionary<string, object> arguments, out HttpRequestExecutionException httpException)
        {
            if (rootException != null && HttpErrorResponseUtility.TryParseErrorResponse(rootException.Number, out int statusCode, out int errorCode, out bool isClientError))
            {
                httpException = new HttpRequestExecutionException((HttpStatusCode)statusCode, errorCode, rootException.Message, isClientError, originalException);
                return true;
            }

            if (HttpStatusCodeDetectionMap.TryGetStatusCode(originalException.AdditionalErrorCode, out HttpErrorResponse defaultResponse))
            {
                HttpErrorResponse error = defaultResponse;
                if (action != null && action.StatusCodeDetectionResponses.TryGetValue(error.StatusCode, out HttpErrorResponse userResponse)) 
                    error = userResponse;

                isClientError = HttpErrorResponseUtility.IsClientError(error.StatusCode);
                string formattedErrorMessage = Regex.Replace(error.ErrorMessage, "{(?<ParameterName>[^}]+)}", x =>
                {
                    string parameterName = x.Groups["ParameterName"].Value;
                    return arguments.TryGetValue(parameterName, out object value) ? value?.ToString() : x.Value;
                });
                httpException = new HttpRequestExecutionException((HttpStatusCode)error.StatusCode, error.ErrorCode, formattedErrorMessage, isClientError, originalException);
                return true;
            }

            httpException = default;
            return false;
        }
    }
}