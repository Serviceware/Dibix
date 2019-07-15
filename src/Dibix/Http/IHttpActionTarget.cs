using System.Reflection;

namespace Dibix.Http
{
    public interface IHttpActionTarget
    {
        MethodInfo Build();
    }
}