using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Dibix.Http.Server.AspNetCore
{
    public interface IHttpActionDelegator
    {
        Task Delegate(HttpContext httpContext, IDictionary<string, object> arguments, CancellationToken cancellationToken);
    }
}