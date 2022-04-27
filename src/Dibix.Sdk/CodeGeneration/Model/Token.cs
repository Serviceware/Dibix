namespace Dibix.Sdk.CodeGeneration
{
    public sealed class Token<T>
    {
        public T Value { get; }
        public string Source { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(T value, string source, int line, int column)
        {
            this.Value = value;
            this.Source = source;
            this.Line = line;
            this.Column = column;
        }

        public static implicit operator T(Token<T> token) => token != null ? token.Value : default;

        public override string ToString() => $"{this.Value} at ({this.Line}, {this.Column})";
    }
}