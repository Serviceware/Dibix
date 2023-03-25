using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public interface IHttpResponseFormatter<in TRequest> where TRequest : IHttpRequestDescriptor
    {
        Task<object> Format(object result, TRequest request, HttpActionDefinition action);
    }
}