using System;

namespace Dibix.Sdk
{
    public class SqlQueryParameter
    {
        public Type ClrType { get; set; }
        public string ClrTypeName { get; set; }
        public string Name { get; set; }
        public string Source { get; set; }
        public string TypeName { get; set; }
        public ContractCheck Check { get; set; }
        public bool IsStructured => !String.IsNullOrEmpty(this.TypeName);
    }
}