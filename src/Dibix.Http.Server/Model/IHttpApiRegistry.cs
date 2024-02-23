using System.Collections.Generic;

namespace Dibix.Http.Server
{
    public interface IHttpApiRegistry
    {
        IEnumerable<HttpApiDescriptor> GetApis();
    }
}