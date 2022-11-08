using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration.CSharp
{
    public sealed class CSharpWriter
    {
        private readonly StringWriter _writer;

        public CSharpRoot Root { get; }

        public CSharpWriter(StringWriter writer, IEnumerable<CSharpAnnotation> globalAnnotations)
        {
            this._writer = writer;
            this.Root = new CSharpRoot(new CSharpStatementScope(), globalAnnotations);
        }

        public string Generate()
        {
            this.Root.Write(this._writer);
            return this._writer.ToString();
        }
    }
}