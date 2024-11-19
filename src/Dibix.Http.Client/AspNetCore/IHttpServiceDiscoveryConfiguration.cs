using System;
using System.Collections.Generic;
using System.Reflection;

namespace Dibix.Http.Client
{
    public interface IHttpServiceDiscoveryConfiguration
    {
        IHttpServiceInfrastructureConfiguration FromAssemblies(IEnumerable<Assembly> assemblies);
        IHttpServiceInfrastructureConfiguration FromAssembly(Assembly assembly);
        IHttpServiceInfrastructureConfiguration FromAssemblyContaining(Type type);
    }
}