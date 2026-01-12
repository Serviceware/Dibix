using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public abstract class HttpActionInvokerBase
    {
        protected static async Task<object> Invoke<TRequest>(HttpActionDefinition action, TRequest request, HttpResponseFormatter<TRequest> responseFormatter, IDictionary<string, object> arguments, IControllerActivator controllerActivator, IParameterDependencyResolver parameterDependencyResolver, CancellationToken cancellationToken) where TRequest : IHttpRequestDescriptor
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

        private static async Task<object> InvokeCore<TRequest>(HttpActionDefinition action, TRequest request, HttpResponseFormatter<TRequest> responseFormatter, IDictionary<string, object> arguments, IControllerActivator controllerActivator, IParameterDependencyResolver parameterDependencyResolver, CancellationToken cancellationToken) where TRequest : IHttpRequestDescriptor
        {
            if (action.Authorization.Any())
            {
                foreach (HttpAuthorizationDefinition authorizationDefinition in action.Authorization)
                {
                    _ = await Execute(authorizationDefinition, request, arguments, controllerActivator, parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
                }
            }

            object result = null;
            try
            {
                result = await Execute(action, request, arguments, controllerActivator, parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
                object formattedResult = await responseFormatter.Format(result, request, action, cancellationToken).ConfigureAwait(false);
                return formattedResult;
            }
            finally
            {
                if (result is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private static async Task<object> Execute(IHttpActionExecutionDefinition definition, IHttpRequestDescriptor request, IDictionary<string, object> arguments, IControllerActivator controllerActivator, IParameterDependencyResolver parameterDependencyResolver, CancellationToken cancellationToken)
        {
            definition.ParameterResolver.PrepareParameters(request, arguments, parameterDependencyResolver);
            object result = await definition.Executor.Execute(controllerActivator, arguments, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}