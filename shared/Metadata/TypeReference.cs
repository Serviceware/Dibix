namespace Dibix
{
    public abstract class TypeReference
    {
        public bool IsNullable { get; set; }
        public bool IsEnumerable { get; }
        public abstract string DisplayName { get; }
        public SourceLocation Location { get; }

        protected TypeReference(bool isNullable, bool isEnumerable, SourceLocation location)
        {
            IsNullable = isNullable;
            IsEnumerable = isEnumerable;
            Location = location;
        }
    }
}