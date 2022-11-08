namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ActionParameterPropertySourceNode
    {
        public ObjectSchema Schema { get; }
        public ObjectSchemaProperty Property { get; }

        public ActionParameterPropertySourceNode(ObjectSchema schema, ObjectSchemaProperty property)
        {
            this.Schema = schema;
            this.Property = property;
        }
    }
}