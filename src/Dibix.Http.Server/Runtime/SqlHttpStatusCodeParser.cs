using System;
using System.Collections.Generic;
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
        internal static bool TryParse(DatabaseAccessException exception, HttpActionDefinition action, IDictionary<string, object> arguments, out HttpRequestExecutionException httpException)
        {
            if (exception.SqlErrorNumber != null && exception.InnerException != null && HttpErrorResponseUtility.TryParseErrorResponse(exception.SqlErrorNumber.Value, out int statusCode, out int errorCode, out bool isClientError))
            {
                httpException = new HttpRequestExecutionException((HttpStatusCode)statusCode, errorCode, exception.InnerException.Message, isClientError, exception);
                return true;
            }

            if (HttpStatusCodeDetectionMap.TryGetStatusCode(exception.AdditionalErrorCode, out HttpErrorResponse defaultResponse))
            {
                HttpErrorResponse error = defaultResponse;
                if (action != null && action.StatusCodeDetectionResponses.TryGetValue(error.StatusCode, out HttpErrorResponse userResponse)) 
                    error = userResponse;

                isClientError = HttpErrorResponseUtility.IsClientError(error.StatusCode);
                IDictionary<string, object> caseInsensitiveArguments = new Dictionary<string, object>(arguments, StringComparer.OrdinalIgnoreCase);
                string formattedErrorMessage = Regex.Replace(error.ErrorMessage, "{(?<ParameterName>[^}]+)}", x =>
                {
                    string parameterName = x.Groups["ParameterName"].Value;
                    return caseInsensitiveArguments.TryGetValue(parameterName, out object value) ? value?.ToString() : x.Value;
                });
                httpException = new HttpRequestExecutionException((HttpStatusCode)error.StatusCode, error.ErrorCode, formattedErrorMessage, isClientError, exception);
                return true;
            }

            httpException = default;
            return false;
        }
    }
}