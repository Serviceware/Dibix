using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host
{
    internal sealed class HttpActionDelegator : IHttpActionDelegator
    {
        private readonly IControllerActivator _controllerActivator;
        private readonly IParameterDependencyResolver _parameterDependencyResolver;
        private readonly ILogger<HttpActionDelegator> _logger;

        public HttpActionDelegator(IControllerActivator controllerActivator, IParameterDependencyResolver parameterDependencyResolver, ILogger<HttpActionDelegator> logger)
        {
            _controllerActivator = controllerActivator;
            _parameterDependencyResolver = parameterDependencyResolver;
            _logger = logger;
        }

        public async Task Delegate(HttpContext httpContext, IDictionary<string, object> arguments, CancellationToken cancellationToken)
        {
            EndpointDefinition endpointDefinition = httpContext.GetEndpointDefinition();
            HttpActionDefinition actionDefinition = endpointDefinition.ActionDefinition;
            IHttpResponseFormatter<HttpRequestDescriptor> responseFormatter = new HttpResponseFormatter(httpContext.Response);
            try
            {
                _ = await HttpActionInvoker.Invoke(actionDefinition, new HttpRequestDescriptor(httpContext.Request), responseFormatter, arguments, _controllerActivator, _parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
            }
            catch (HttpRequestExecutionException httpRequestExecutionException)
            {
                if (!httpRequestExecutionException.IsClientError)
                    _logger.LogError(httpRequestExecutionException, httpRequestExecutionException.ErrorMessage);

                httpRequestExecutionException.AppendToResponse(httpContext.Response);
            }
        }
    }
}