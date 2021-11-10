namespace Dibix.Sdk.CodeGeneration
{
    public sealed class DefaultValue
    {
        public object Value { get; }
        public string Source { get; }
        public int Line { get; }
        public int Column { get; }

        public DefaultValue(object value, string source, int line, int column)
        {
            this.Value = value;
            this.Source = source;
            this.Line = line;
            this.Column = column;
        }
    }
}