using System;
using System.Collections.Generic;

namespace Dibix.Http
{
    public interface IHttpParameterResolutionMethod
    {
        string Source { get; }
        IDictionary<string, Type> Parameters { get; }

        void PrepareParameters(IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver);
    }
}