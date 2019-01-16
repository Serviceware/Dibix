namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CSharpField : CSharpStatement
    {
        private readonly string _name;
        private readonly string _type;
        private readonly CSharpValue _value;
        private readonly CSharpModifiers _modifiers;

        public CSharpField(string name, string type, CSharpValue value, CSharpModifiers modifiers)
        {
            this._name = name;
            this._type = type;
            this._value = value;
            this._modifiers = modifiers;
        }

        public override void Write(StringWriter writer)
        {
            base.Write(writer);
            WriteModifiers(writer, this._modifiers);

            writer.WriteRaw(this._type)
                  .WriteRaw(' ')
                  .WriteRaw(this._name)
                  .WriteRaw(" = ");

            this._value.Write(writer);

            writer.WriteRaw(';');
        }
    }
}