using System;

namespace Dibix.Http
{
    internal static class HttpErrorResponseParser
    {
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

            isClientError = statusCode % 400 < 100;
            bool isServerError = statusCode % 500 < 100;

            return isServerError || isClientError;
        }
    }
}