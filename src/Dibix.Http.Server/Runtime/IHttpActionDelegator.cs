using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dibix.Http.Server
{
    public interface IHttpActionDelegator
    {
        Task Delegate(IDictionary<string, object> arguments);
    }
}