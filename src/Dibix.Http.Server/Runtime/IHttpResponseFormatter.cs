using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public abstract class HttpResponseFormatter<TRequest> where TRequest : IHttpRequestDescriptor
    {
        // Responses may be stored in a private cache, even if the request carried credentials.
        // Without this, a token swap could replay the cached representation of another principal.
        protected const string ETagVary = "Authorization";

        public abstract Task<object> Format(object result, TRequest request, HttpActionDefinition action, CancellationToken cancellationToken);

        protected static string GetContentDispositionType(ContentDispositionType dispositionType) => dispositionType switch
        {
            ContentDispositionType.Inline => "inline",
            ContentDispositionType.Attachment => "attachment",
            _ => throw new ArgumentOutOfRangeException(nameof(dispositionType), dispositionType, null)
        };

        // Returns the formatted entity tag for a result, whose contract is marked with '$containsEtag', or for a 'THROW 304000, @etag, 1'
        protected static string GetEntityTag(object result) => result is IETagProvider etagProvider ? $"\"{etagProvider.ETag}\"" : null;
    }
}