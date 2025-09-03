using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public abstract class HttpResponseFormatter<TRequest> where TRequest : IHttpRequestDescriptor
    {
        public abstract Task<object> Format(object result, TRequest request, HttpActionDefinition action, CancellationToken cancellationToken);

        protected static string GetContentDispositionType(ContentDispositionType dispositionType) => dispositionType switch
        {
            ContentDispositionType.Inline => "inline",
            ContentDispositionType.Attachment => "attachment",
            _ => throw new ArgumentOutOfRangeException(nameof(dispositionType), dispositionType, null)
        };
    }
}