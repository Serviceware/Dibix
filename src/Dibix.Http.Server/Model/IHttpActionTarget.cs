using System.Reflection;

namespace Dibix.Http.Server
{
    public interface IHttpActionTarget
    {
        MethodInfo Build();
    }
}