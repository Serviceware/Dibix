using System;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class TypeLoaderFacade : ITypeLoaderFacade
    {
        #region Fields
        private readonly ITypeLoader _innerTypeLoader;
        private readonly ITypeLoader _runtimeTypeLoader;
        #endregion

        #region Constructor
        public TypeLoaderFacade(ITypeLoader innerTypeLoader, IAssemblyLocator assemblyLocator)
        {
            this._innerTypeLoader = innerTypeLoader;
            this._runtimeTypeLoader = new RuntimeTypeLoader(assemblyLocator);
        }
        #endregion

        #region ITypeLoaderFacade Members
        public TypeInfo LoadType(string typeName, Action<string> errorHandler)
        {
            TypeName parsedTypeName = typeName;

            if (!parsedTypeName.IsAssemblyQualified)
            {
                TypeInfo clrType = TryClrType(parsedTypeName);
                if (clrType != null)
                    return clrType;
            }

            ITypeLoader resolvedTypeLoader = parsedTypeName.IsAssemblyQualified ? this._runtimeTypeLoader : this._innerTypeLoader;
            return resolvedTypeLoader.LoadType(parsedTypeName, errorHandler);
        }

        private static TypeInfo TryClrType(TypeName parsedTypeName)
        {
            // Try CSharp type name first (string => System.String)
            Type type = parsedTypeName.NormalizedTypeName.ToClrType();
            if (type != null)
                return new TypeInfo(parsedTypeName, true);

            type = Type.GetType(parsedTypeName.NormalizedTypeName);
            if (type != null && type.IsPrimitive())
            {
                parsedTypeName.CSharpTypeName = type.ToCSharpTypeName();
                return new TypeInfo(parsedTypeName, true);
            }

            return null;
        }
        #endregion
    }
}