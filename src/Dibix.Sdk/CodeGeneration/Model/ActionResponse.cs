using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionResponse
    {
        public HttpStatusCode StatusCode { get; }
        public string MediaType { get; }
        public bool IsBinary { get; }
        public TypeReference ResultType { get; set; }
        public string Description { get; set; }
        public ICollection<ErrorDescription> Errors { get; }

        public ActionResponse(HttpStatusCode statusCode) : this(statusCode, mediaType: HttpMediaType.Default, isBinary: false, resultType: null) { }
        public ActionResponse(HttpStatusCode statusCode, TypeReference resultType) : this(statusCode, mediaType: HttpMediaType.Default, isBinary: false, resultType) { }
        public ActionResponse(HttpStatusCode statusCode, string mediaType, bool isBinary) : this(statusCode, mediaType, isBinary, resultType: null) { }
        private ActionResponse(HttpStatusCode statusCode, string mediaType, bool isBinary, TypeReference resultType)
        {
            this.Errors = new Collection<ErrorDescription>();
            this.StatusCode = statusCode;
            this.MediaType = mediaType;
            this.IsBinary = isBinary;
            this.ResultType = resultType;
        }
    }
}