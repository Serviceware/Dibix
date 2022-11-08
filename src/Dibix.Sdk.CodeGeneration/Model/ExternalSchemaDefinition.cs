using System;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class ExternalSchemaDefinition
    {
        public ExternalSchemaOwner Owner { get; }
        public SchemaDefinition SchemaDefinition { get; }

        public ExternalSchemaDefinition(ExternalSchemaOwner owner, SchemaDefinition schemaDefinition)
        {
            this.Owner = owner;
            this.SchemaDefinition = schemaDefinition;
        }

        public TSchema GetSchema<TSchema>() where TSchema : SchemaDefinition
        {
            if (!(this.SchemaDefinition is TSchema typedSchema))
                throw new InvalidOperationException($"Schema '{this.SchemaDefinition.FullName}' is not of expected type '{typeof(TSchema)}'");

            return typedSchema;
        }
    }
}