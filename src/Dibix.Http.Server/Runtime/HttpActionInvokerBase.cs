using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public abstract class HttpActionInvokerBase
    {
        protected static async Task<object> Invoke<TRequest>(HttpActionDefinition action, TRequest request, IHttpResponseFormatter<TRequest> responseFormatter, IDictionary<string, object> arguments, IControllerActivator controllerActivator, IParameterDependencyResolver parameterDependencyResolver, CancellationToken cancellationToken) where TRequest : IHttpRequestDescriptor
        {
            try
            {
                return await InvokeCore(action, request, responseFormatter, arguments, controllerActivator, parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
            }
            catch (DatabaseAccessException exception)
            {
                // Sample:
                // THROW 404017, N'Feature not configured', 1
                // 404017 => 404 17 => HttpStatusCode.NotFound (ResultCode: 17) - ResultCode can be a more specific application/feature error code
                // 
                // HTTP/1.1 404 NotFound
                // X-Result-Code: 17
                if (SqlHttpStatusCodeParser.TryParse(exception, action, arguments, out HttpRequestExecutionException httpException))
                    throw httpException;

                throw;
            }
        }
        
        private static async Task<object> InvokeCore<TRequest>(HttpActionDefinition action, TRequest request, IHttpResponseFormatter<TRequest> responseFormatter, IDictionary<string, object> arguments, IControllerActivator controllerActivator, IParameterDependencyResolver parameterDependencyResolver, CancellationToken cancellationToken) where TRequest : IHttpRequestDescriptor
        {
            if (action.Authorization.Any())
            {
                foreach (HttpAuthorizationDefinition authorizationDefinition in action.Authorization)
                {
                    // Clone the arguments, so they don't overwrite the endpoint arguments.
                    // For example having a 'productid' parameter in both authorization behavior and endpoint with different meanings and different types can cause collisions.
                    IDictionary<string, object> authorizationArguments = arguments.ToDictionary(x => x.Key, x => x.Value);
                    _ = await Execute(authorizationDefinition, request, authorizationArguments, controllerActivator, parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
                }
            }

            object result = await Execute(action, request, arguments, controllerActivator, parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
            object formattedResult = await responseFormatter.Format(result, request, action, cancellationToken).ConfigureAwait(false);
            return formattedResult;
        }

        private static async Task<object> Execute(IHttpActionExecutionDefinition definition, IHttpRequestDescriptor request, IDictionary<string, object> arguments, IControllerActivator controllerActivator, IParameterDependencyResolver parameterDependencyResolver, CancellationToken cancellationToken)
        {
            definition.ParameterResolver.PrepareParameters(request, arguments, parameterDependencyResolver);
            object result = await definition.Executor.Execute(controllerActivator, arguments, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}