namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class CSharpParameter : CSharpStatement
    {
        private readonly string _name;
        private readonly string _type;

        public CSharpParameter(string name, string type)
        {
            this._name = name;
            this._type = type;
        }

        public override void Write(StringWriter writer)
        {
            writer.WriteRaw(this._type)
                  .WriteRaw(' ')
                  .WriteRaw(this._name);
        }
    }
}