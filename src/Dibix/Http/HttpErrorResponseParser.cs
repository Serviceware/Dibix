﻿using System;
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

        public static bool TryParseErrorResponse(ref string errorMessage, int errorNumber, out int statusCode, out int errorCode)
        {
            string originalErrorMessage = errorMessage;

            errorMessage = null;
            statusCode = 0;
            errorCode = 0;
            if (errorNumber == 0)
                return false;

            const int expectedDigitLength = 6;
            int length = (int)Math.Log10(Math.Abs(errorNumber)) + 1;
            if (length != expectedDigitLength)
                return false;

            int parsedStatusCode = errorNumber / 1000;
            int parsedErrorCode = errorNumber % 1000;

            bool isServerError = ServerErrorHttpStatuses.Contains(parsedStatusCode) && errorCode == 0;
            bool isClientError = ClientErrorHttpStatuses.Contains(parsedStatusCode);

            if (!isServerError && !isClientError)
                return false;

            statusCode = parsedStatusCode;

            if (isClientError)
            {
                if (parsedErrorCode != 0)
                    errorCode = parsedErrorCode;

                errorMessage = originalErrorMessage;
            }

            return true;
        }
    }
}