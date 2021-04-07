namespace Dibix.Sdk.CodeGeneration.CSharp
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

        protected override void WriteBody(StringWriter writer)
        {
            WriteModifiers(writer, this._modifiers);

            writer.WriteRaw(this._type)
                  .WriteRaw(' ')
                  .WriteRaw(this._name);

            if (this._value != null)
            {
                writer.WriteRaw(" = ");
                this._value.Write(writer);
            }

            writer.WriteRaw(';');
        }
    }
}