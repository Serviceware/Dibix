namespace Dibix.Sdk.CodeGeneration
{
    public abstract class TypeReference
    {
        public bool IsNullable { get; }
        public bool IsEnumerable { get; }

        protected TypeReference(bool isNullable, bool isEnumerable)
        {
            this.IsNullable = isNullable;
            this.IsEnumerable = isEnumerable;
        }
    }
}