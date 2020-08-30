namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal sealed class CSharpStringValue : CSharpValue
    {
        private readonly bool _verbatim;

        public CSharpStringValue(string value, bool verbatim) : base(value)
        {
            this._verbatim = verbatim;
        }

        public override void Write(StringWriter writer)
        {
            if (this._verbatim)
                writer.WriteRaw('@');

            writer.WriteRaw('"');

            base.Write(writer);

            writer.WriteRaw('"');
        }

        protected override string FormatValue(string value) => SanitizeValue(this._verbatim, value);

        internal static string SanitizeValue(bool verbatim, string value)
        {
            string sanitized = value;
            if (!verbatim)
                sanitized = sanitized.Replace("\\", "\\\\")     // Escape \
                                     .Replace("\r\n", "\\r\\n") // Escape line breaks
                                     .Replace("\"", "\\\"");    // Escape "
            else
                sanitized = sanitized.Replace("\"", "\"\"");    // Escape "
            
            return sanitized;
        }
    }
}