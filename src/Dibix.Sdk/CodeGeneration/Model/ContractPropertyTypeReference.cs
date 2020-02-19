namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ContractPropertyTypeReference : ContractPropertyType
    {
        public string TypeName { get; }

        public ContractPropertyTypeReference(string typeName, bool isEnumerable, bool isNullable) : base(isEnumerable, isNullable)
        {
            this.TypeName = typeName;
        }
    }
}