using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Server
{
    public interface IHttpActionDelegator
    {
        Task Delegate(HttpContext httpContext, IDictionary<string, object> arguments);
    }
}