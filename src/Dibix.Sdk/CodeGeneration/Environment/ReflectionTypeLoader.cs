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

        public static CodeGeneration.TypeInfo GetTypeInfo(TypeName typeName, string assemblyPath)
        {
            AppDomain domain = null;
            try
            {
                domain = AppDomain.CreateDomain($"Dibix.Sdk.Reflection: {Guid.NewGuid()}", null, new AppDomainSetup { ApplicationBase = AssemblyDirectory });
                ReflectionTypeLoader instance = (ReflectionTypeLoader)domain.CreateInstanceAndUnwrap(AssemblyName, TypeName);
                TypeInfo info = instance.GetTypeInfo(typeName.AssemblyName, assemblyPath, typeName.NormalizedTypeName);
                typeName.CSharpTypeName = info.CSharpTypeName;
                CodeGeneration.TypeInfo result = new CodeGeneration.TypeInfo(typeName, info.IsPrimitive);
                result.Properties.AddRange(info.PropertyNames);
                return result;
            }
            finally
            {
                if (domain != null)
                    AppDomain.Unload(domain);
            }
        }

        private TypeInfo GetTypeInfo(string assemblyName, string assemblyPath, string typeName)
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