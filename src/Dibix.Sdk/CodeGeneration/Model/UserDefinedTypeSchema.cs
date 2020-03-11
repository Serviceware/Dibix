namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeSchema : ObjectSchema
    {
        public string UdtTypeName { get; }

        public UserDefinedTypeSchema(string @namespace, string definitionName, string udtTypeName) : base(@namespace, definitionName)
        {
            this.UdtTypeName = udtTypeName;
        }
    }
}