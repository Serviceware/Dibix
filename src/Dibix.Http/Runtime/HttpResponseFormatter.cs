using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Dibix.Http
{
    internal abstract class HttpResponseFormatter
    {
        private static readonly ICollection<HttpResponseFormatter> Formatters = new HttpResponseFormatter[]
        {
            new HttpFileResponseFormatter()
          , new WrappedHttpResponseFormatter()
        };

        protected abstract bool CanFormatResult(HttpActionDefinition actionDefinition, object result);
        protected abstract object FormatResult(HttpActionDefinition actionDefinition, object result, HttpRequestMessage requestMessage);

        public static object Format(HttpActionDefinition actionDefinition, object result, HttpRequestMessage requestMessage)
        {
            foreach (HttpResponseFormatter formatter in Formatters.Where(x => x.CanFormatResult(actionDefinition, result)))
                return formatter.FormatResult(actionDefinition, result, requestMessage);

            return result;
        }
    }
}