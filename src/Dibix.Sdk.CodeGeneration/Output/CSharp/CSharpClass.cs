using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public sealed class CSharpClass : CSharpStatement
    {
        private readonly string _name;
        private readonly IList<CSharpExpression> _members;
        private readonly CSharpModifiers _modifiers;
        private readonly ICollection<string> _implementedInterfaces;
        private string _baseClassName;

        public CSharpClass(string name, CSharpModifiers modifiers, IEnumerable<CSharpAnnotation> annotations) : base(annotations)
        {
            this._name = name;
            this._modifiers = modifiers;
            this._members = new Collection<CSharpExpression>();
            this._implementedInterfaces = new Collection<string>();
        }

        public CSharpClass AddField(string name, string type, CSharpValue value = null, CSharpModifiers modifiers = CSharpModifiers.Public)
        {
            CSharpField constant = new CSharpField(name, type, value, modifiers);
            this._members.Add(constant);
            return this;
        }

        public CSharpProperty AddProperty(string name, string returnType, CSharpModifiers modifiers = CSharpModifiers.Public) => this.AddProperty(name, returnType, Enumerable.Empty<CSharpAnnotation>(), modifiers);
        public CSharpProperty AddProperty(string name, string returnType, IEnumerable<CSharpAnnotation> annotations, CSharpModifiers modifiers = CSharpModifiers.Public)
        {
            CSharpProperty property = new CSharpProperty(name, returnType, modifiers, annotations);
            this._members.Add(property);
            return property;
        }

        public CSharpConstructor AddConstructor(string body = null, CSharpModifiers modifiers = CSharpModifiers.Public)
        {
            CSharpConstructor ctor = new CSharpConstructor(this._name, body, modifiers);
            this._members.Add(ctor);
            return ctor;
        }

        public CSharpMethod AddMethod(string name, string returnType, string body, bool isExtension = false, CSharpModifiers modifiers = CSharpModifiers.Public) => this.AddMethod(name, returnType, body, Enumerable.Empty<CSharpAnnotation>(), isExtension, modifiers);
        public CSharpMethod AddMethod(string name, string returnType, string body, IEnumerable<CSharpAnnotation> annotations, bool isExtension = false, CSharpModifiers modifiers = CSharpModifiers.Public)
        {
            CSharpMethod method = new CSharpMethod(name, returnType, body, isExtension, modifiers, annotations);
            this._members.Add(method);
            return method;
        }

        public CSharpClass AddClass(string name, CSharpModifiers modifiers = CSharpModifiers.Public)
        {
            CSharpClass @class = new CSharpClass(name, modifiers, Enumerable.Empty<CSharpAnnotation>());
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

        public CSharpClass Implements(string interfaceName)
        {
            this._implementedInterfaces.Add(interfaceName);
            return this;
        }

        protected override void WriteBody(StringWriter writer)
        {
            WriteModifiers(writer, this._modifiers);

            writer.WriteRaw("class ")
                  .WriteRaw(this._name);

            IList<string> bases = this._implementedInterfaces.ToList();
            if (!String.IsNullOrEmpty(this._baseClassName))
                bases.Insert(0, this._baseClassName);

            if (bases.Any())
                writer.WriteRaw(" : ");

            for (int i = 0; i < bases.Count; i++)
            {
                string @base = bases[i];
                writer.WriteRaw(@base);

                if (i + 1 < bases.Count)
                    writer.WriteRaw(", ");
            }

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