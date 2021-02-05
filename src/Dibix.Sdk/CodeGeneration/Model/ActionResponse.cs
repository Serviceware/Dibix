using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ActionResponse
    {
        public HttpStatusCode StatusCode { get; }
        public TypeReference ResultType { get; set; }
        public string Description { get; set; }
        public ICollection<ErrorDescription> Errors { get; }

        public ActionResponse(HttpStatusCode statusCode)
        {
            this.StatusCode = statusCode;
            this.Errors = new Collection<ErrorDescription>();
        }
        public ActionResponse(HttpStatusCode statusCode, TypeReference resultType) : this(statusCode)
        {
            this.ResultType = resultType;
        }
    }
}