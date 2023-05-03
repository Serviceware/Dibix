using System.Collections.Generic;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionResponse
    {
        public HttpStatusCode StatusCode { get; }
        public string MediaType { get; } = HttpMediaType.Json;
        public TypeReference ResultType { get; set; }
        public string Description { get; set; }
        public ErrorDescription StatusCodeDetectionDetail { get; set; }
        public IDictionary<int, ErrorDescription> Errors { get; } = new Dictionary<int, ErrorDescription>();

        public ActionResponse(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }
        public ActionResponse(HttpStatusCode statusCode, TypeReference resultType) : this(statusCode)
        {
            ResultType = resultType;
        }
        public ActionResponse(HttpStatusCode statusCode, string mediaType, TypeReference resultType) : this(statusCode)
        {
            MediaType = mediaType;
            ResultType = resultType;
        }
    }
}