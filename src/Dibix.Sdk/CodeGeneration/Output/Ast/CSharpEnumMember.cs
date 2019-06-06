namespace Dibix.Sdk.CodeGeneration
{
    public sealed class CSharpEnumMember : CSharpStatement
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
            base.Write(writer);

            writer.Write(this._name);
            if (this._value != null)
            {
                writer.WriteRaw(" = ")
                      .WriteRaw(this._value);
            }
        }
    }
}