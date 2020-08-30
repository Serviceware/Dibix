namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public class CSharpValue : CSharpStatement
    {
        private readonly string _value;

        public CSharpValue(string value)
        {
            this._value = value;
        }

        public override void Write(StringWriter writer)
        {
            string value = this.FormatValue(this._value);
            writer.WriteRaw(value);
        }

        protected virtual string FormatValue(string value) => value;
    }
}