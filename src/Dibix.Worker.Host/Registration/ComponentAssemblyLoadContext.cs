using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Dibix.Worker.Host
{
    internal sealed class ComponentAssemblyLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public ComponentAssemblyLoadContext(string name, string componentAssemblyPath) : base(name)
        {
            _resolver = new AssemblyDependencyResolver(componentAssemblyPath);
        }

        protected override Assembly? Load(AssemblyName name)
        {
            Assembly? assembly = All.SelectMany(x => x.Assemblies).FirstOrDefault(x => x.GetName().ToString() == name.ToString());
            if (assembly == null)
            {
                string? assemblyPath = _resolver.ResolveAssemblyToPath(name);
                if (assemblyPath != null)
                    assembly = LoadFromAssemblyPath(assemblyPath);
            }
            return assembly;
        }
    }
}