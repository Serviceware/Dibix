namespace Dibix.Sdk.CodeGeneration
{
    public class Token<T>
    {
        public T Value { get; }
        public string Source { get; }
        public int Line { get; }
        public int Column { get; }

        public Token(T value, string source, int line, int column)
        {
            Guard.IsNotNull(source, nameof(source));
            this.Value = value;
            this.Source = source;
            this.Line = line;
            this.Column = column;
        }

        public static implicit operator T(Token<T> token) => token != null ? token.Value : default;

        //public override string ToString() => $"{this.Value} at ({this.Line}, {this.Column})";
        public override string ToString() => this.Value.ToString();
    }
}