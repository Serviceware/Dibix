using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dibix.Http.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Dibix.Http.Host
{
    internal sealed class DefaultEndpointImplementationProvider : IEndpointImplementationProvider
    {
        public Delegate GetImplementation(EndpointDefinition endpoint)
        {
            // TODO: Compile to delegate
            async Task Implementation(HttpContext context/*, ... the actual expected parameters from the target method */)
            {
                DatabaseScope scope = context.RequestServices.GetRequiredService<DatabaseScope>();
                scope.InitiatorFullName = endpoint.Definition.Executor.Method.Name;
                IParameterDependencyResolver parameterDependencyResolver = context.RequestServices.GetRequiredService<IParameterDependencyResolver>();
                IDictionary<string, object> arguments = CollectArguments(context);
                IHttpResponseFormatter<HttpRequestDescriptor> responseFormatter = new HttpResponseFormatter(context.Response);
                _ = await HttpActionInvoker.Invoke(endpoint.Definition, new HttpRequestDescriptor(context.Request), responseFormatter, arguments, parameterDependencyResolver).ConfigureAwait(false);
            }

            return Implementation;
        }

        private static IDictionary<string, object> CollectArguments(HttpContext context)
        {
            IDictionary<string, object> arguments = new Dictionary<string, object>();
            return arguments;
        }
    }
}