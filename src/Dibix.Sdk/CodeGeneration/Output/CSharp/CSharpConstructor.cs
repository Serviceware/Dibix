using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public sealed class CSharpConstructor : CSharpStatement
    {
        private readonly string _declaringTypeName;
        private readonly IList<CSharpParameter> _parameters;
        private readonly string _body;
        private readonly string _baseConstructorParameters;
        private readonly CSharpModifiers _modifiers;

        public CSharpConstructor(string declaringTypeName, string body, string baseConstructorParameters, CSharpModifiers modifiers)
        {
            this._declaringTypeName = declaringTypeName;
            this._body = body;
            this._baseConstructorParameters = baseConstructorParameters;
            this._modifiers = modifiers;
            this._parameters = new Collection<CSharpParameter>();
        }

        public CSharpConstructor AddParameter(string name, string type)
        {
            CSharpParameter parameter = new CSharpParameter(name, type, Enumerable.Empty<string>());
            this._parameters.Add(parameter);
            return this;
        }

        public override void Write(StringWriter writer)
        {
            base.Write(writer);
            WriteModifiers(writer, this._modifiers);

            writer.WriteRaw(this._declaringTypeName)
                  .WriteRaw('(');

            for (int i = 0; i < this._parameters.Count; i++)
            {
                CSharpParameter parameter = this._parameters[i];
                parameter.Write(writer);
                if (i + 1 < this._parameters.Count)
                    writer.WriteRaw(", ");
            }

            writer.WriteRaw(')');

            if (!String.IsNullOrEmpty(this._baseConstructorParameters))
            {
                writer.WriteRaw(" : base(")
                      .WriteRaw(this._baseConstructorParameters)
                      .WriteRaw(')');
            }

            writer.WriteLine()
                  .WriteLine("{")
                  .PushIndent();

            WriteMultiline(writer, this._body);

            writer.PopIndent()
                  .Write("}");
        }
    }
}