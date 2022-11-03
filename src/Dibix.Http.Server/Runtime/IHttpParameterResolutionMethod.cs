using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;

namespace Dibix.Http.Server
{
    public interface IHttpParameterResolutionMethod
    {
        MethodInfo Method { get; }
        string Source { get; }
        IDictionary<string, HttpActionParameter> Parameters { get; }

        void PrepareParameters(HttpRequestMessage request, IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver);
    }
}