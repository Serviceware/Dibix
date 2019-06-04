namespace Dibix.Sdk.CodeGeneration
{
    public sealed class CSharpWriter
    {
        private readonly StringWriter _writer;

        public CSharpRoot Root { get; }

        public CSharpWriter(StringWriter writer, string @namespace)
        {
            this._writer = writer;
            this.Root = new CSharpRoot(@namespace);
        }

        public string Generate()
        {
            this.Root.Write(this._writer);
            return this._writer.ToString();
        }
    }
}