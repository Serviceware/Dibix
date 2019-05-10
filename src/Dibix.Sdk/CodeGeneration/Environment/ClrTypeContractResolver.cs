using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ClrTypeContractResolver : IContractResolver
    {
        #region IContractResolver Members
        public ContractInfo ResolveContract(string input, Action<string> errorHandler)
        {
            bool isAssemblyQualified = input.IndexOf(',') >= 0;
            if (isAssemblyQualified)
                return null;

            ContractName name = new ContractName(input);

            // Try CSharp type name first (string => System.String)
            Type type = name.TypeName.ToClrType();
            if (type != null)
                return new ContractInfo(name, true);

            type = Type.GetType(name.TypeName);
            if (type == null || !type.IsPrimitive())
                return null;

            name.TypeName = type.ToCSharpTypeName();
            return new ContractInfo(name, true);
        }
        #endregion
    }
}