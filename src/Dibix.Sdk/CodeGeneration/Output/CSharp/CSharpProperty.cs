using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public sealed class CSharpProperty : CSharpStatement
    {
        private readonly string _name;
        private readonly string _returnType;
        private readonly CSharpModifiers _modifiers;
        private CSharpPropertyGetter _getter;
        private CSharpPropertySetter _setter;
        private CSharpValue _initializer;

        public CSharpProperty(string name, string returnType, CSharpModifiers modifiers, IEnumerable<CSharpAnnotation> annotations) : base(annotations)
        {
            this._name = name;
            this._returnType = returnType;
            this._modifiers = modifiers;
        }

        public CSharpProperty Getter(string body, CSharpModifiers modifiers = CSharpModifiers.None)
        {
            this._getter = new CSharpPropertyGetter(body, modifiers);
            return this;
        }

        public CSharpProperty Setter(string body, CSharpModifiers modifiers = CSharpModifiers.None)
        {
            this._setter = new CSharpPropertySetter(body, modifiers);
            return this;
        }

        public CSharpProperty Initializer(CSharpValue value)
        {
            this._initializer = value;
            return this;
        }

        protected override void WriteBody(StringWriter writer)
        {
            WriteModifiers(writer, this._modifiers);

            writer.WriteRaw(this._returnType)
                  .WriteRaw(' ')
                  .WriteRaw(this._name);

            bool isMultiline = Equals(this._getter?.HasBody, true) && Equals(this._setter?.HasBody, true)
                            || Equals(this._getter?.HasMultilineBody, true)
                            || Equals(this._setter?.HasMultilineBody, true);

            if (isMultiline)
            {
                writer.WriteLine()
                      .WriteLine("{")
                      .PushIndent();
            }
            else
                writer.WriteRaw(" { ");

            if (this._getter != null)
            {
                if (isMultiline)
                    writer.WriteIndent();

                this._getter.Write(writer);
            }

            if (this._getter != null && this._setter != null)
            {
                if (isMultiline)
                    writer.WriteLine();
                else
                    writer.WriteRaw(' ');
            }

            if (this._setter != null)
            {
                if (isMultiline)
                    writer.WriteIndent();

                this._setter.Write(writer);

            }

            if (isMultiline)
            {
                writer.WriteLine()
                      .PopIndent()
                      .Write("}");
            }
            else
            {
                writer.WriteRaw(" }");
            }

            if (this._initializer != null)
            {
                bool isAutoProperty = !Equals(this._getter?.HasBody, true) && !Equals(this._setter?.HasBody, true);
                if (!isAutoProperty)
                    throw new InvalidOperationException("Property initializers are only supported for auto properties");

                writer.WriteRaw(" = ");

                this._initializer.Write(writer);

                writer.WriteRaw(';');
            }
        }
    }
}