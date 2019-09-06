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
    }
}