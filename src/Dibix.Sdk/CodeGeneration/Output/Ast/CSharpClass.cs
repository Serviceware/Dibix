using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class CSharpClass : CSharpStatement
    {
        private readonly string _name;
        private readonly IList<CSharpStatement> _members;
        private readonly CSharpModifiers _modifiers;
        private string _baseClassName;

        public CSharpClass(string name, CSharpModifiers modifiers, IEnumerable<string> annotations) : base(annotations)
        {
            this._name = name;
            this._modifiers = modifiers;
            this._members = new Collection<CSharpStatement>();
        }

        public CSharpClass AddField(string name, string type, CSharpValue value, CSharpModifiers modifiers = CSharpModifiers.Public)
        {
            CSharpField constant = new CSharpField(name, type, value, modifiers);
            this._members.Add(constant);
            return this;
        }

        public CSharpProperty AddProperty(string name, string returnType, CSharpModifiers modifiers = CSharpModifiers.Public) => this.AddProperty(name, returnType, Enumerable.Empty<string>(), modifiers);
        public CSharpProperty AddProperty(string name, string returnType, IEnumerable<string> annotations, CSharpModifiers modifiers = CSharpModifiers.Public)
        {
            CSharpProperty property = new CSharpProperty(name, returnType, modifiers, annotations);
            this._members.Add(property);
            return property;
        }

        public CSharpConstructor AddConstructor(string body, CSharpModifiers modifiers = CSharpModifiers.Public, string baseConstructorParameters = null)
        {
            CSharpConstructor ctor = new CSharpConstructor(this._name, body, baseConstructorParameters, modifiers);
            this._members.Add(ctor);
            return ctor;
        }

        public CSharpMethod AddMethod(string name, string type, string body, bool isExtension = false, CSharpModifiers modifiers = CSharpModifiers.Public)
        {
            CSharpMethod method = new CSharpMethod(name, type, body, isExtension, modifiers);
            this._members.Add(method);
            return method;
        }

        public CSharpClass AddClass(string name, CSharpModifiers modifiers = CSharpModifiers.Public)
        {
            CSharpClass @class = new CSharpClass(name, modifiers, Enumerable.Empty<string>());
            this._members.Add(@class);
            return @class;
        }

        public CSharpClass AddComment(string comment, bool isMultiline)
        {
            this._members.Add(new CSharpComment(comment, isMultiline));
            return this;
        }

        public CSharpClass AddSeparator()
        {
            this._members.Add(new CSharpSeparator());
            return this;
        }

        public CSharpClass Inherits(string baseClassName)
        {
            this._baseClassName = baseClassName;
            return this;
        }

        public override void Write(StringWriter writer)
        {
            base.Write(writer);
            WriteModifiers(writer, this._modifiers);

            writer.WriteRaw("class ")
                  .WriteRaw(this._name);

            if (!String.IsNullOrEmpty(this._baseClassName))
            {
                writer.WriteRaw(" : ")
                      .WriteRaw(this._baseClassName);
            }

            writer.WriteLine()
                  .WriteLine("{")
                  .PushIndent();

            for (int i = 0; i < this._members.Count; i++)
            {
                CSharpStatement member = this._members[i];
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