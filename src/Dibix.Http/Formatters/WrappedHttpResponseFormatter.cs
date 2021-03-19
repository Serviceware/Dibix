using System.Net.Http;

namespace Dibix.Http
{
    internal sealed class WrappedHttpResponseFormatter : HttpResponseFormatter
    {
        protected override bool CanFormatResult(HttpActionDefinition actionDefinition, object result) => result is HttpResponse;

        protected override object FormatResult(HttpActionDefinition actionDefinition, object result, HttpRequestMessage requestMessage) => ((HttpResponse)result).CreateResponse(requestMessage);
    }
}