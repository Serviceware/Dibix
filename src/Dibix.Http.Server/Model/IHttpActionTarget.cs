using System.Reflection;

namespace Dibix.Http.Server
{
    public interface IHttpActionTarget
    {
        bool IsExternal { get; }
        MethodInfo Build();
    }
}