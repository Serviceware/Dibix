using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ValueReference<TType> : ValueReference where TType : TypeReference
    {
        public new TType Type => (TType)base.Type;

        protected ValueReference(TType type, SourceLocation location) : base(type, location)
        {
        }
    }

    public abstract class ValueReference
    {
        public TypeReference Type { get; }
        public SourceLocation Location { get; }

        protected ValueReference(TypeReference type, SourceLocation location)
        {
            Type = type;
            Location = location;
        }
    }
}