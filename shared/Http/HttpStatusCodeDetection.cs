using System.Collections.Generic;
using System.Linq;

namespace Dibix.Http
{
    internal static class HttpStatusCodeDetection
    {
        public static readonly IReadOnlyDictionary<DatabaseAccessErrorCode, HttpErrorResponse> StatusCodeMap = new Dictionary<DatabaseAccessErrorCode, HttpErrorResponse>
        {
            [DatabaseAccessErrorCode.SequenceContainsNoElements] = new HttpErrorResponse(404, errorCode: 0, "The entity could not be found")
        };
        public static IDictionary<int, HttpErrorResponse> HttpStatusCodeMap { get; } = StatusCodeMap.Values.ToDictionary(x => x.StatusCode);
        public static IReadOnlyDictionary<DatabaseAccessErrorCode, HttpErrorResponse> DatabaseErrorCodeHttpStatusMap { get; } = StatusCodeMap;

        public static bool TryGetStatusCode(DatabaseAccessErrorCode errorCode, out HttpErrorResponse mapping) => StatusCodeMap.TryGetValue(errorCode, out mapping);
    }
}