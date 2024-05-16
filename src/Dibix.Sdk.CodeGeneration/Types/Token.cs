namespace Dibix.Sdk.CodeGeneration
{
    public class Token<T>
    {
        public T Value { get; }
        public SourceLocation Location { get; }

        public Token(T value, SourceLocation location)
        {
            Value = value;
            Location = location;
        }

        public static implicit operator T(Token<T> token) => token != null ? token.Value : default;

        //public override string ToString() => $"{Value} at ({Line}, {Column})";
        public override string ToString() => Value.ToString();
    }
}