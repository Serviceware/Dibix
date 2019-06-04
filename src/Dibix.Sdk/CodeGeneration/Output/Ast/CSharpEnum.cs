using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class CSharpEnum : CSharpStatement
    {
        private readonly string _name;
        private readonly IList<CSharpEnumMember> _members;
        private readonly CSharpModifiers _modifiers;
        private string _baseTypeName;

        public CSharpEnum(string name, CSharpModifiers modifiers, string annotation = null) : base(annotation)
        {
            this._name = name;
            this._modifiers = modifiers;
            this._members = new Collection<CSharpEnumMember>();
        }

        public CSharpEnum AddMember(string name, int? value)
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

        public override void Write(StringWriter writer)
        {
            base.Write(writer);
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
                CSharpStatement member = this._members[i];
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