using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Dibix.Http.Host
{
    internal sealed class HttpActionDelegator : IHttpActionDelegator
    {
        private readonly DatabaseScope _databaseScope;
        private readonly IParameterDependencyResolver _parameterDependencyResolver;
        private readonly ILogger<HttpActionDelegator> _logger;

        public HttpActionDelegator(DatabaseScope databaseScope, IParameterDependencyResolver parameterDependencyResolver, ILogger<HttpActionDelegator> logger)
        {
            _databaseScope = databaseScope;
            _parameterDependencyResolver = parameterDependencyResolver;
            _logger = logger;
        }

        public async Task Delegate(HttpContext httpContext, IDictionary<string, object> arguments)
        {
            Endpoint? endpoint = httpContext.GetEndpoint();
            if (endpoint == null)
                throw new InvalidOperationException("Could not retrieve endpoint from http context");

            EndpointDefinition? endpointDefinition = endpoint.Metadata.GetMetadata<EndpointDefinition>();
            if (endpointDefinition == null)
                throw new InvalidOperationException("Could not retrieve endpoint definition from endpoint metadata");

            HttpActionDefinition actionDefinition = endpointDefinition.ActionDefinition;
            _databaseScope.InitiatorFullName = actionDefinition.Executor.Method.Name;
            IHttpResponseFormatter<HttpRequestDescriptor> responseFormatter = new HttpResponseFormatter(httpContext.Response);
            try
            {
                _ = await HttpActionInvoker.Invoke(actionDefinition, new HttpRequestDescriptor(httpContext.Request), responseFormatter, arguments, _parameterDependencyResolver).ConfigureAwait(false);
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