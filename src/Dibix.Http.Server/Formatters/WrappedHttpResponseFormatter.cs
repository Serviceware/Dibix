using System.Net.Http;

namespace Dibix.Http.Server
{
    internal sealed class WrappedHttpResponseFormatter : HttpResponseFormatter
    {
        protected override bool CanFormatResult(HttpActionDefinition actionDefinition, object result) => result is HttpResponse;

        protected override object FormatResult(HttpActionDefinition actionDefinition, object result, HttpRequestMessage requestMessage) => ((HttpResponse)result).CreateResponse(requestMessage);
    }
}