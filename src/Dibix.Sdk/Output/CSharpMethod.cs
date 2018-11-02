using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk
{
    internal sealed class CSharpMethod : CSharpStatement
    {
        private readonly string _name;
        private readonly string _returnType;
        private readonly IList<CSharpParameter> _parameters;
        private readonly string _body;
        private readonly bool _isExtension;
        private readonly CSharpModifiers _modifiers;

        public CSharpMethod(string name, string returnType, string body, bool isExtension, CSharpModifiers modifiers)
        {
            this._name = name;
            this._returnType = returnType;
            this._body = body;
            this._isExtension = isExtension;
            this._modifiers = modifiers;
            this._parameters = new Collection<CSharpParameter>();
        }

        public CSharpMethod AddParameter(string name, string type)
        {
            CSharpParameter parameter = new CSharpParameter(name, type);
            this._parameters.Add(parameter);
            return this;
        }

        public override void Write(StringWriter writer)
        {
            WriteModifiers(writer, this._modifiers);

            writer.WriteRaw(this._returnType)
                  .WriteRaw(' ')
                  .WriteRaw(this._name)
                  .WriteRaw('(');

            if (this._isExtension)
                writer.WriteRaw("this ");

            for (int i = 0; i < this._parameters.Count; i++)
            {
                CSharpParameter parameter = this._parameters[i];
                parameter.Write(writer);
                if (i + 1 < this._parameters.Count)
                    writer.WriteRaw(", ");
            }

            writer.WriteRaw(')')
                  .WriteLine()
                  .WriteLine("{")
                  .PushIndent();

            WriteMultiline(writer, this._body);

            writer.PopIndent()
                  .Write("}");
        }
    }
}