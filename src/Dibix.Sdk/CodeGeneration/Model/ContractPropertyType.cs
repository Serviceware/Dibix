namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ContractPropertyType
    {
        public bool IsEnumerable { get; }
        public bool IsNullable { get; }

        protected ContractPropertyType(bool isEnumerable, bool isNullable)
        {
            this.IsEnumerable = isEnumerable;
            this.IsNullable = isNullable;
        }
    }
}