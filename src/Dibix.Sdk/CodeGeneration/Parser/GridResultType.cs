namespace Dibix.Sdk.CodeGeneration
{
    public sealed class GridResultType
    {
        public Namespace Namespace { get; }
        public string TypeName { get; }

        public GridResultType(Namespace @namespace, string typeName)
        {
            this.Namespace = @namespace;
            this.TypeName = typeName;
        }
    }
}