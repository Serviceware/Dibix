namespace Dibix.Sdk.CodeGeneration
{
    public sealed class UserDefinedTypeSchema : ObjectSchema
    {
        public string UdtName { get; }

        public UserDefinedTypeSchema(string @namespace, string definitionName, string udtName) : base(@namespace, definitionName)
        {
            this.UdtName = udtName;
        }
    }
}