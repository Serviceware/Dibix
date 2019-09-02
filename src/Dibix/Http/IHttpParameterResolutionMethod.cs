using System;
using System.Collections.Generic;

namespace Dibix.Http
{
    public interface IHttpParameterResolutionMethod
    {
        IDictionary<string, Type> Parameters { get; }

        void PrepareParameters(IDictionary<string, object> arguments, IParameterDependencyResolver dependencyResolver);
    }
}