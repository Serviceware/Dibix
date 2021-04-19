using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public sealed class CSharpInterface : CSharpStatement
    {
        private readonly string _name;
        private readonly IList<CSharpExpression> _members;
        private readonly CSharpModifiers _modifiers;

        public CSharpInterface(string name, CSharpModifiers modifiers)
        {
            this._name = name;
            this._modifiers = modifiers;
            this._members = new Collection<CSharpExpression>();
        }

        public CSharpMethod AddMethod(string name, string returnType)
        {
            CSharpMethod method = new CSharpMethod(name, returnType, body: null, isExtension: false, modifiers: CSharpModifiers.None, annotations: Enumerable.Empty<CSharpAnnotation>());
            this._members.Add(method);
            return method;
        }

        protected override void WriteBody(StringWriter writer)
        {
            WriteModifiers(writer, this._modifiers);

            writer.WriteRaw("interface ")
                  .WriteRaw(this._name);

            writer.WriteLine()
                  .WriteLine("{")
                  .PushIndent();

            for (int i = 0; i < this._members.Count; i++)
            {
                CSharpExpression member = this._members[i];
                member.Write(writer);
                if (i + 1 < this._members.Count)
                    writer.WriteLine();
            }

            writer.WriteLine()
                  .PopIndent()
                  .Write("}");
        }
    }
}