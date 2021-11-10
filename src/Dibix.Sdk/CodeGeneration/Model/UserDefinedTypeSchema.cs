namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeSchema : ObjectSchema
    {
        public string UdtName { get; }

        public UserDefinedTypeSchema(string @namespace, string definitionName, SchemaDefinitionSource source, string udtName) : base(@namespace, definitionName, source)
        {
            this.UdtName = udtName;
        }
    }
}