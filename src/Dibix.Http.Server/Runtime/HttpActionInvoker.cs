using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public static class HttpActionInvoker
    {
        public static Task<object> Invoke(HttpActionDefinition action, HttpRequestMessage request, IDictionary<string, object> arguments, IParameterDependencyResolver parameterDependencyResolver, CancellationToken cancellationToken)
        {
            IHttpResponseFormatter<HttpRequestMessageDescriptor> responseFormatter = new HttpResponseMessageFormatter();
            return Invoke(action, new HttpRequestMessageDescriptor(request), responseFormatter, arguments, parameterDependencyResolver, cancellationToken);
        }
        public static async Task<object> Invoke<TRequest>(HttpActionDefinition action, TRequest request, IHttpResponseFormatter<TRequest> responseFormatter, IDictionary<string, object> arguments, IParameterDependencyResolver parameterDependencyResolver, CancellationToken cancellationToken) where TRequest : IHttpRequestDescriptor
        {
            try
            {
                if (action.Authorization != null)
                {
                    _ = await Execute(action.Authorization, request, arguments, parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
                }

                object result = await Execute(action, request, arguments, parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
                object formattedResult = await responseFormatter.Format(result, request, action, cancellationToken).ConfigureAwait(false);
                return formattedResult;
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

        private static async Task<object> Execute(IHttpActionExecutionDefinition definition, IHttpRequestDescriptor request, IDictionary<string, object> arguments, IParameterDependencyResolver parameterDependencyResolver, CancellationToken cancellationToken)
        {
            definition.ParameterResolver.PrepareParameters(request, arguments, parameterDependencyResolver);
            object result = await definition.Executor.Execute(arguments, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }
}