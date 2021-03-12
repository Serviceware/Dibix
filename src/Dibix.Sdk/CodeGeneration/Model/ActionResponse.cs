using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionResponse
    {
        public HttpStatusCode StatusCode { get; }
        public string MediaType { get; } = HttpMediaType.Default;
        public TypeReference ResultType { get; set; }
        public string Description { get; set; }
        public ICollection<ErrorDescription> Errors { get; }

        public ActionResponse(HttpStatusCode statusCode)
        {
            this.Errors = new Collection<ErrorDescription>();
            this.StatusCode = statusCode;
        }
        public ActionResponse(HttpStatusCode statusCode, TypeReference resultType) : this(statusCode)
        {
            this.ResultType = resultType;
        }
        public ActionResponse(HttpStatusCode statusCode, string mediaType, TypeReference resultType) : this(statusCode)
        {
            this.MediaType = mediaType;
            this.ResultType = resultType;
        }
    }
}