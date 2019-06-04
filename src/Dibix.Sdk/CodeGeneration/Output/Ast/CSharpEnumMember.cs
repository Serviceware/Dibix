namespace Dibix.Sdk.CodeGeneration
{
    public sealed class CSharpEnumMember : CSharpStatement
    {
        private readonly string _name;
        private readonly int? _value;

        public CSharpEnumMember(string name, int? value)
        {
            this._name = name;
            this._value = value;
        }

        public override void Write(StringWriter writer)
        {
            base.Write(writer);

            writer.Write(this._name);
            if (this._value.HasValue)
            {
                writer.WriteRaw(" = ")
                      .WriteRaw(this._value);
            }
        }
    }
}