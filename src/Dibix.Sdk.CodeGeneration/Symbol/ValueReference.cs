namespace Dibix.Sdk.CodeGeneration
{
    public abstract class ValueReference<TType> : ValueReference where TType : TypeReference
    {
        public new TType Type => (TType)base.Type;

        protected ValueReference(TType type, string source, int line, int column) : base(type, source, line, column)
        {
        }
    }

    public abstract class ValueReference
    {
        public TypeReference Type { get; }
        public string Source { get; }
        public int Line { get; }
        public int Column { get; }

        protected ValueReference(TypeReference type, string source, int line, int column)
        {
            this.Type = type;
            this.Source = source;
            this.Line = line;
            this.Column = column;
        }
    }
}