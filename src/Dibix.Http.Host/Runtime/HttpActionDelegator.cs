using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Host
{
    internal sealed class HttpActionDelegator : IHttpActionDelegator
    {
        private readonly DatabaseScope _databaseScope;
        private readonly IParameterDependencyResolver _parameterDependencyResolver;
        private readonly HttpContext _httpContext;

        public HttpActionDelegator(DatabaseScope databaseScope, IParameterDependencyResolver parameterDependencyResolver, IHttpContextAccessor httpContextAccessor)
        {
            _databaseScope = databaseScope;
            _parameterDependencyResolver = parameterDependencyResolver;
            _httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("HttpContext not set");
        }

        public async Task Delegate(IDictionary<string, object> arguments)
        {
            Endpoint? endpoint = _httpContext.GetEndpoint();
            if (endpoint == null)
                throw new InvalidOperationException("Could not retrieve endpoint from http context");

            EndpointDefinition? endpointDefinition = endpoint.Metadata.GetMetadata<EndpointDefinition>();
            if (endpointDefinition == null)
                throw new InvalidOperationException("Could not retrieve endpoint definition from endpoint metadata");

            HttpActionDefinition actionDefinition = endpointDefinition.ActionDefinition;
            _databaseScope.InitiatorFullName = actionDefinition.Executor.Method.Name;
            IHttpResponseFormatter<HttpRequestDescriptor> responseFormatter = new HttpResponseFormatter(_httpContext.Response);
            _ = await HttpActionInvoker.Invoke(actionDefinition, new HttpRequestDescriptor(_httpContext.Request), responseFormatter, arguments, _parameterDependencyResolver).ConfigureAwait(false);
        }
    }
}