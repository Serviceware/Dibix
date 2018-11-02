namespace Dibix.Sdk
{
    internal class CSharpValue : CSharpStatement
    {
        private readonly string _value;

        public CSharpValue(string value)
        {
            this._value = value;
        }

        public override void Write(StringWriter writer)
        {
            writer.WriteRaw(this._value);
        }
    }
}