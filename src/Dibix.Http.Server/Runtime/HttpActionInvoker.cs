using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public static class HttpActionInvoker
    {
        public static Task<object> Invoke(HttpActionDefinition action, HttpRequestMessage request, IDictionary<string, object> arguments, IParameterDependencyResolver parameterDependencyResolver)
        {
            IHttpResponseFormatter<HttpRequestMessageDescriptor> responseFormatter = new HttpResponseMessageFormatter();
            return Invoke(action, new HttpRequestMessageDescriptor(request), responseFormatter, arguments, parameterDependencyResolver);
        }
        public static async Task<object> Invoke<TRequest>(HttpActionDefinition action, TRequest request, IHttpResponseFormatter<TRequest> responseFormatter, IDictionary<string, object> arguments, IParameterDependencyResolver parameterDependencyResolver) where TRequest : IHttpRequestDescriptor
        {
            try
            {
                if (action.Authorization != null)
                {
                    _ = await Execute(action.Authorization, request, arguments, parameterDependencyResolver).ConfigureAwait(false);
                }

                object result = await Execute(action, request, arguments, parameterDependencyResolver).ConfigureAwait(false);
                object formattedResult = await responseFormatter.Format(result, request, action).ConfigureAwait(false);
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
                if (TryParseHttpError(action, arguments, exception, exception.InnerException as SqlException, out HttpRequestExecutionException httpException))
                    throw httpException;

                throw;
            }
        }

        private static async Task<object> Execute(IHttpActionExecutionDefinition definition, IHttpRequestDescriptor request, IDictionary<string, object> arguments, IParameterDependencyResolver parameterDependencyResolver)
        {
            definition.ParameterResolver.PrepareParameters(request, arguments, parameterDependencyResolver);
            object result = await definition.Executor.Execute(arguments).ConfigureAwait(false);
            return result;
        }

        private static bool TryParseHttpError(HttpActionDefinition action, IDictionary<string, object> arguments, DatabaseAccessException originalException, SqlException rootException, out HttpRequestExecutionException httpException)
        {
            if (rootException != null && HttpErrorResponseUtility.TryParseErrorResponse(rootException.Number, out int statusCode, out int errorCode, out bool isClientError))
            {
                httpException = new HttpRequestExecutionException((HttpStatusCode)statusCode, errorCode, rootException.Message, isClientError, originalException);
                return true;
            }

            if (HttpStatusCodeDetectionMap.TryGetStatusCode(originalException.AdditionalErrorCode, out HttpErrorResponse defaultResponse) && action.StatusCodeDetectionResponses.TryGetValue(defaultResponse.StatusCode, out HttpErrorResponse userResponse))
            {
                isClientError = HttpErrorResponseUtility.IsClientError(defaultResponse.StatusCode);
                string errorMessage = userResponse.ErrorMessage ?? defaultResponse.ErrorMessage;
                string formattedErrorMessage = Regex.Replace(errorMessage, "{(?<ParameterName>[^}]+)}", x =>
                {
                    string parameterName = x.Groups["ParameterName"].Value;
                    return arguments.TryGetValue(parameterName, out object value) ? value?.ToString() : x.Value;
                });
                httpException = new HttpRequestExecutionException((HttpStatusCode)defaultResponse.StatusCode, userResponse.ErrorCode, formattedErrorMessage, isClientError, originalException);
                return true;
            }

            httpException = default;
            return false;
        }
    }
}