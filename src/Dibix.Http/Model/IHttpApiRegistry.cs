using System.Collections.Generic;
using System.Reflection;

namespace Dibix.Http
{
    public interface IHttpApiRegistry
    {
        string GetAreaName(Assembly assembly);

        IEnumerable<HttpApiDescriptor> GetCustomApis();
    }
}