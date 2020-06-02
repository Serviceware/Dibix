using System;
using System.Linq;
using System.Net;

namespace Dibix.Http
{
    internal static class HttpErrorResponseParser
    {
        public const string ClientErrorCodeHeaderName = "X-Error-Code";
        public const string ClientErrorDescriptionHeaderName = "X-Error-Description";

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

        public static bool TryParseErrorResponse(int errorNumber, out int statusCode, out int errorCode, out bool isClientError)
        {
            statusCode = 0;
            errorCode = 0;
            isClientError = false;
            if (errorNumber == 0)
                return false;

            const int expectedDigitLength = 6;
            int length = (int)Math.Log10(Math.Abs(errorNumber)) + 1;
            if (length != expectedDigitLength)
                return false;

            statusCode = errorNumber / 1000;
            errorCode = errorNumber % 1000;

            bool isServerError = ServerErrorHttpStatuses.Contains(statusCode) && errorCode == 0;
            isClientError = ClientErrorHttpStatuses.Contains(statusCode);

            return isServerError || isClientError;
        }
    }
}