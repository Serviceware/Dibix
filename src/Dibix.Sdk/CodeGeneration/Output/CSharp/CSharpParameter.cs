using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    internal sealed class CSharpParameter : CSharpStatement
    {
        private readonly string _name;
        private readonly string _type;

        public CSharpParameter(string name, string type, IEnumerable<string> annotations) : base(annotations)
        {
            this._name = name;
            this._type = type;
        }

        public override void Write(StringWriter writer)
        {
            base.Write(writer);
            writer.WriteRaw(this._type)
                  .WriteRaw(' ')
                  .WriteRaw(this._name);
        }

        protected override void WriteAnnotation(StringWriter writer, string annotation)
        {
            writer.WriteRaw($"[{annotation}] ");
        }
    }
}