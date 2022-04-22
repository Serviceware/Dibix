namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ExternalSchemaOwner
    {
        public string DefaultClassName { get; }

        public ExternalSchemaOwner(string defaultClassName)
        {
            this.DefaultClassName = defaultClassName;
        }
    }
}