using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Dibix.Http.Server.AspNetCore;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host
{
    internal sealed class HttpActionDelegator : HttpActionInvokerBase, IHttpActionDelegator
    {
        private readonly IControllerActivator _controllerActivator;
        private readonly IParameterDependencyResolver _parameterDependencyResolver;

        public HttpActionDelegator(IControllerActivator controllerActivator, IParameterDependencyResolver parameterDependencyResolver)
        {
            _controllerActivator = controllerActivator;
            _parameterDependencyResolver = parameterDependencyResolver;
        }

        public async Task Delegate(HttpContext httpContext, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            EndpointDefinition endpointDefinition = httpContext.GetEndpointDefinition();
            HttpActionDefinition actionDefinition = endpointDefinition.ActionDefinition;
            IHttpResponseFormatter<HttpRequestDescriptor> responseFormatter = new HttpResponseFormatter(httpContext.Response);
            _ = await Invoke(actionDefinition, new HttpRequestDescriptor(httpContext.Request), responseFormatter, arguments, _controllerActivator, _parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
        }
    }
}