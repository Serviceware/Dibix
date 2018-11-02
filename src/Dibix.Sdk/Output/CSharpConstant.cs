namespace Dibix.Sdk
{
    internal sealed class CSharpConstant : CSharpStatement
    {
        private readonly string _name;
        private readonly string _type;
        private readonly CSharpValue _value;
        private readonly CSharpModifiers _modifiers;

        public CSharpConstant(string name, string type, CSharpValue value, CSharpModifiers modifiers)
        {
            this._name = name;
            this._type = type;
            this._value = value;
            this._modifiers = modifiers;
        }

        public override void Write(StringWriter writer)
        {
            WriteModifiers(writer, this._modifiers);

            writer.WriteRaw("const ")
                  .WriteRaw(this._type)
                  .WriteRaw(' ')
                  .WriteRaw(this._name)
                  .WriteRaw(" = ");

            this._value.Write(writer);

            writer.WriteRaw(';');
        }
    }
}