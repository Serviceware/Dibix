namespace Dibix.Sdk.Abstractions
{
    public readonly struct SourceLocation
    {
        public string Source { get; }
        public int Line { get; }
        public int Column { get; }

        public SourceLocation(string source, int line, int column)
        {
            Source = source;
            Line = line;
            Column = column;
        }
    }
}