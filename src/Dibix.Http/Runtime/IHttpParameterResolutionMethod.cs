using System.Collections.Generic;
using System.Net.Http;

namespace Dibix.Http
{
    public interface IHttpParameterResolutionMethod
    {
        string Source { get; }
        IDictionary<string, HttpActionParameter> Parameters { get; }

        void PrepareParameters(HttpRequestMessage request, IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver);
    }
}