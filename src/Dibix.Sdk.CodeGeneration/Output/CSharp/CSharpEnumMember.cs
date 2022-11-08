namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public sealed class CSharpEnumMember : CSharpExpression
    {
        private readonly string _name;
        private readonly string _value;

        public CSharpEnumMember(string name, string value)
        {
            this._name = name;
            this._value = value;
        }

        public override void Write(StringWriter writer)
        {
            writer.Write(this._name);
            if (this._value != null)
            {
                writer.WriteRaw(" = ")
                      .WriteRaw(this._value);
            }
        }
    }
}