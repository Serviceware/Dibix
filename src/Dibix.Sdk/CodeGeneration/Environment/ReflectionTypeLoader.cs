using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ReflectionTypeLoader : MarshalByRefObject
    {
        private static readonly string AssemblyDirectory = Path.GetDirectoryName(typeof(ReflectionTypeLoader).Assembly.Location);
        private static readonly string AssemblyName = typeof(ReflectionTypeLoader).Assembly.FullName;
        private static readonly string TypeName = typeof(ReflectionTypeLoader).FullName;

        public static ContractInfo GetTypeInfo(string assemblyName, ContractName contractName, string assemblyPath)
        {
            AppDomain domain = null;
            try
            {
                domain = AppDomain.CreateDomain($"Dibix.Sdk.Reflection: {Guid.NewGuid()}", null, new AppDomainSetup { ApplicationBase = AssemblyDirectory });

                // Not sure yet why I need this..
                // Might have to do something with different assembly load contexts
                // Both assemblies/types get compared before they can be casted
                // If something does not compare, you'll end up with invalid cast from transparent proxy..
                ResolveEventHandler onAssemblyResolve = (sender, e) =>
                {
                    if (e.Name == AssemblyName)
                        return AppDomain.CurrentDomain.GetAssemblies().Single(x => x.FullName == AssemblyName);

                    return null;
                };
                AppDomain.CurrentDomain.AssemblyResolve += onAssemblyResolve;
                ReflectionTypeLoader instance = (ReflectionTypeLoader)domain.CreateInstanceAndUnwrap(AssemblyName, TypeName);
                AppDomain.CurrentDomain.AssemblyResolve -= onAssemblyResolve;

                TypeInfo info = instance.GetTypeInfo(assemblyName, contractName.TypeName, assemblyPath);
                contractName.TypeName = info.CSharpTypeName;
                ContractInfo result = new ContractInfo(contractName, info.IsPrimitive);
                result.Properties.AddRange(info.PropertyNames);
                return result;
            }
            finally
            {
                if (domain != null)
                    AppDomain.Unload(domain);
            }
        }

        private TypeInfo GetTypeInfo(string assemblyName, string typeName, string assemblyPath)
        {
            Assembly assembly = !String.IsNullOrEmpty(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : Assembly.Load(assemblyName);
            Type type = assembly.GetType(typeName, true);
            TypeInfo info = new TypeInfo(type.IsPrimitive(), type.ToCSharpTypeName(), type.GetProperties().Select(x => x.Name));
            return info;
        }

        private class TypeInfo : MarshalByRefObject
        {
            public bool IsPrimitive { get; }
            public string CSharpTypeName { get; }
            public ICollection<string> PropertyNames { get; }

            public TypeInfo(bool isPrimitive, string cSharpTypeName, IEnumerable<string> propertyNames)
            {
                this.IsPrimitive = isPrimitive;
                this.CSharpTypeName = cSharpTypeName;
                this.PropertyNames = new ReadOnlyCollection<string>(propertyNames.ToArray());
            }
        }
    }
}