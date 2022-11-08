using System;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public sealed class CSharpNamespaceScope : CSharpStatementScope
    {
        private readonly string _namespace;

        internal CSharpNamespaceScope(string @namespace)
        {
            Guard.IsNotNullOrEmpty(@namespace, nameof(@namespace));
            this._namespace = @namespace;
        }

        public override void Write(StringWriter writer)
        {
            writer.WriteLine(String.Concat("namespace ", this._namespace))
                  .WriteLine("{")
                  .PushIndent();

            base.Write(writer);

            writer.PopIndent()
                  .WriteLine()
                  .Write("}");
        }
    }
}