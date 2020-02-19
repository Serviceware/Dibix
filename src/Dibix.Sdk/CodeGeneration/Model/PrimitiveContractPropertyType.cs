namespace Dibix.Sdk.CodeGeneration
{
    public sealed class PrimitiveContractPropertyType : ContractPropertyType
    {
        public ContractPropertyDataType Type { get; }

        public PrimitiveContractPropertyType(ContractPropertyDataType type, bool isEnumerable, bool isNullable) : base(isEnumerable, isNullable)
        {
            this.Type = type;
        }
    }
}