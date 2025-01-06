using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Server.AspNet
{
    public class HttpActionInvoker : HttpActionInvokerBase
    {
        // ASP.NET implementation
        // Uses custom exception handling
        public static async Task<object> Invoke(HttpActionDefinition action, HttpRequestMessage request, IDictionary<string, object> arguments, IControllerActivator controllerActivator, IParameterDependencyResolver parameterDependencyResolver, CancellationToken cancellationToken)
        {
            IHttpResponseFormatter<HttpRequestMessageDescriptor> responseFormatter = new HttpResponseMessageFormatter();
            return await Invoke(action, new HttpRequestMessageDescriptor(request), responseFormatter, arguments, controllerActivator, parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
        }
    }
}