namespace Dibix.Sdk.CodeGeneration
{
    public abstract class TypeReference
    {
        public bool IsNullable { get; set; }
        public bool IsEnumerable { get; }
        public string Source { get; }
        public int Line { get; }
        public int Column { get; }
        public abstract string DisplayName { get; }

        protected TypeReference(bool isNullable, bool isEnumerable, string source, int line, int column)
        {
            this.IsNullable = isNullable;
            this.IsEnumerable = isEnumerable;
            this.Source = source;
            this.Line = line;
            this.Column = column;
        }
    }
}