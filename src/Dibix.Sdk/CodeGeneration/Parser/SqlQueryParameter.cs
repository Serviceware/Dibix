using System;

namespace Dibix.Sdk.CodeGeneration
{
    public class SqlQueryParameter
    {
        public Type ClrType { get; set; }
        public string ClrTypeName { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public ContractCheck Check { get; set; }
        public bool IsStructured => !String.IsNullOrEmpty(this.TypeName);
        public bool Obfuscate { get; set; }
    }
}