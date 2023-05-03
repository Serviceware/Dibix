using System.Collections.Generic;
using System.Linq;

namespace Dibix.Http.Server
{
    internal static class HttpStatusCodeDetectionMap
    {
        private static readonly IDictionary<DatabaseAccessErrorCode, HttpErrorResponse> StatusCodeMap = new Dictionary<DatabaseAccessErrorCode, HttpErrorResponse>
        {
            [DatabaseAccessErrorCode.SequenceContainsNoElements] = new HttpErrorResponse(404, "The entity could not be found")
        };

        public static Dictionary<int, HttpErrorResponse> Defaults { get; } = StatusCodeMap.Values.ToDictionary(x => x.StatusCode);

        public static bool TryGetStatusCode(DatabaseAccessErrorCode errorCode, out HttpErrorResponse mapping) => StatusCodeMap.TryGetValue(errorCode, out mapping);
    }
}