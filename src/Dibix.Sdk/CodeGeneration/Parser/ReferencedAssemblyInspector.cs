using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ReferencedAssemblyInspector : AssemblyResolver
    {
        public T Inspect<T>(Func<IEnumerable<Assembly>, T> referencedAssembliesHandler) => ReflectionOnlyTypeInspector.Inspect(() =>
        {
            T result = referencedAssembliesHandler(this.GetReferencedAssemblies().Select(base.LoadAssembly));
            return result;
        });

        protected abstract IEnumerable<string> GetReferencedAssemblies();
    }
}