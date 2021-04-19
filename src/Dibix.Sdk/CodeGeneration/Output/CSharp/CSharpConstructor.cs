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
        private readonly CSharpModifiers _modifiers;
        private CSharpConstructorInvocationExpression _invocation;

        public CSharpConstructor(string declaringTypeName, string body, CSharpModifiers modifiers)
        {
            this._declaringTypeName = declaringTypeName;
            this._body = body;
            this._modifiers = modifiers;
            this._parameters = new Collection<CSharpParameter>();
        }

        public CSharpConstructor AddParameter(string name, string type)
        {
            CSharpParameter parameter = new CSharpParameter(name, type, default, null, Enumerable.Empty<CSharpAnnotation>());
            this._parameters.Add(parameter);
            return this;
        }

        public ICSharpConstructorInvocationExpression CallThis() => this.Call(CtorInvocation.This);

        public ICSharpConstructorInvocationExpression CallBase() => this.Call(CtorInvocation.Base);

        protected override void WriteBody(StringWriter writer)
        {
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

            if (this._invocation != null)
            {
                writer.WriteRaw($" : {ResolveCtorInvocation(this._invocation.Invocation)}(");

                for (int i = 0; i < this._invocation.Parameters.Count; i++)
                {
                    CSharpValue parameter = this._invocation.Parameters[i];
                    parameter.Write(writer);

                    if (i + 1 < this._invocation.Parameters.Count)
                        writer.WriteRaw(", ");
                }

                writer.WriteRaw(')');
            }

            bool hasBody = !String.IsNullOrEmpty(this._body);

            if (hasBody)
            {
                writer.WriteLine()
                      .WriteIndent();
            }
            else
                writer.WriteRaw(' ');

            writer.WriteRaw("{");

            if (hasBody)
            {
                writer.WriteLine()
                      .PushIndent();

                WriteMultiline(writer, this._body);

                writer.PopIndent()
                      .WriteIndent();
            }
            else
            {
                writer.WriteRaw(' ');
            }

            writer.WriteRaw("}");
        }

        private ICSharpConstructorInvocationExpression Call(CtorInvocation invocation)
        {
            this._invocation = new CSharpConstructorInvocationExpression(invocation);
            return this._invocation;
        }

        private static string ResolveCtorInvocation(CtorInvocation invocation)
        {
            switch (invocation)
            {
                case CtorInvocation.Base: return "base";
                case CtorInvocation.This: return "this";
                default: throw new ArgumentOutOfRangeException(nameof(invocation), invocation, null);
            }
        }

        private sealed class CSharpConstructorInvocationExpression : ICSharpConstructorInvocationExpression
        {
            public CtorInvocation Invocation { get; }
            public IList<CSharpValue> Parameters { get; }

            public CSharpConstructorInvocationExpression(CtorInvocation invocation)
            {
                this.Invocation = invocation;
                this.Parameters = new Collection<CSharpValue>();
            }

            public ICSharpConstructorInvocationExpression AddParameter(CSharpValue value)
            {
                this.Parameters.Add(value);
                return this;
            }
        }

        private enum CtorInvocation
        {
            None,
            Base,
            This
        }
    }
}