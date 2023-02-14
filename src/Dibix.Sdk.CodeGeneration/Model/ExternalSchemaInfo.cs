namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ExternalSchemaInfo
    {
        public ExternalSchemaOwner Owner { get; }

        public ExternalSchemaInfo(ExternalSchemaOwner owner)
        {
            this.Owner = owner;
        }
    }
}