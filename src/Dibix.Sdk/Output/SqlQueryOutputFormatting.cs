namespace Dibix.Sdk
{
    public enum SqlQueryOutputFormatting
    {
        None = 0,
        Verbatim = 1,
        StripDoubleQuotes = 2,
        Minified = 4,
        WhiteStripped = 8,
        Singleline = StripDoubleQuotes | Minified,
        Multiline = Verbatim | StripDoubleQuotes
    }
}