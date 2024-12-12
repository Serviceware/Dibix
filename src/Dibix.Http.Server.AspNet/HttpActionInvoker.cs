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
            try
            {
                return await Invoke(action, new HttpRequestMessageDescriptor(request), responseFormatter, arguments, controllerActivator, parameterDependencyResolver, cancellationToken).ConfigureAwait(false);
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
    }
}