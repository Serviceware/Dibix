using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public interface IHttpActionExecutionMethod
    {
        MethodInfo Method { get; }
        string Source { get; }

        Task<object> Execute(IDictionary<string, object> arguments);
    }
}