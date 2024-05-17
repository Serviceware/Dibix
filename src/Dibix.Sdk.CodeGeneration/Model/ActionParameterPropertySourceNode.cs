namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterPropertySourceNode
    {
        public ObjectSchema Schema { get; }
        public IPropertyDescriptor Property { get; }

        public ActionParameterPropertySourceNode(ObjectSchema schema, IPropertyDescriptor property)
        {
            this.Schema = schema;
            this.Property = property;
        }
    }
}