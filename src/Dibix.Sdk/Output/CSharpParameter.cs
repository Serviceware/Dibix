namespace Dibix.Sdk
{
    internal sealed class CSharpParameter : CSharpStatement
    {
        private readonly string _name;
        private readonly string _type;
        private readonly string _annotation;

        public CSharpParameter(string name, string type, string annotation)
        {
            this._name = name;
            this._type = type;
            this._annotation = annotation;
        }

        public override void Write(StringWriter writer)
        {
            if (this._annotation != null)
                writer.WriteRaw(this._annotation)
                      .WriteRaw(' ');

            writer.WriteRaw(this._type)
                  .WriteRaw(' ')
                  .WriteRaw(this._name);
        }
    }
}