using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public sealed class CSharpEnum : CSharpStatement
    {
        private readonly string _name;
        private readonly IList<CSharpEnumMember> _members;
        private readonly CSharpModifiers _modifiers;
        private string _baseTypeName;

        public CSharpEnum(string name, CSharpModifiers modifiers, IEnumerable<CSharpAnnotation> annotations) : base(annotations)
        {
            this._name = name;
            this._modifiers = modifiers;
            this._members = new Collection<CSharpEnumMember>();
        }

        public CSharpEnum AddMember(string name, string value)
        {
            CSharpEnumMember member = new CSharpEnumMember(name, value);
            this._members.Add(member);
            return this;
        }

        public CSharpEnum Inherits(string baseTypeName)
        {
            this._baseTypeName = baseTypeName;
            return this;
        }

        protected override void WriteBody(StringWriter writer)
        {
            WriteModifiers(writer, this._modifiers);

            writer.WriteRaw("enum ")
                  .WriteRaw(this._name);

            if (!String.IsNullOrEmpty(this._baseTypeName))
            {
                writer.WriteRaw(" : ")
                      .WriteRaw(this._baseTypeName);
            }

            writer.WriteLine()
                  .WriteLine("{")
                  .PushIndent();

            for (int i = 0; i < this._members.Count; i++)
            {
                CSharpExpression member = this._members[i];
                member.Write(writer);
                if (i + 1 < this._members.Count)
                    writer.WriteRaw(',')
                          .WriteLine();
            }

            writer.WriteLine()
                  .PopIndent()
                  .Write("}");
        }
    }
}