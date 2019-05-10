using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CoreTypeLoader : ITypeLoader
    {
        #region ITypeLoader Members
        public TypeInfo LoadType(TypeName typeName, Action<string> errorHandler)
        {
            TypeName parsedTypeName = typeName;

            if (parsedTypeName.IsAssemblyQualified)
                return null;

            TypeInfo clrType = TryClrType(parsedTypeName);
            return clrType;
        }
        #endregion

        #region Private Methods
        private static TypeInfo TryClrType(TypeName parsedTypeName)
        {
            // Try CSharp type name first (string => System.String)
            Type type = parsedTypeName.NormalizedTypeName.ToClrType();
            if (type != null)
                return new TypeInfo(parsedTypeName, true);

            type = Type.GetType(parsedTypeName.NormalizedTypeName);
            if (type == null || !type.IsPrimitive())
                return null;

            parsedTypeName.CSharpTypeName = type.ToCSharpTypeName();
            return new TypeInfo(parsedTypeName, true);
        }
        #endregion
    }
}