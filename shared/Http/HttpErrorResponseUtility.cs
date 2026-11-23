using System;

namespace Dibix.Http
{
    internal static class HttpErrorResponseUtility
    {
        public const int NotModifiedStatusCode = 304;

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

            isClientError = IsClientError(statusCode);
            bool isServerError = statusCode % 500 < 100;

            // 304 is not an error, but the success path of a conditional GET (If-None-Match).
            // It is matched explicitly instead of widening the client/server error ranges above.
            bool isNotModified = statusCode == NotModifiedStatusCode;

            return isServerError || isClientError || isNotModified;
        }

        public static bool IsClientError(int statusCode) => statusCode % 400 < 100;
    }
}