using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class ObjectSchema : SchemaDefinition
    {
        public string WcfNamespace { get; }
        public IReadOnlyList<ObjectSchemaProperty> Properties { get; }

        public ObjectSchema(string @namespace, string definitionName, SchemaDefinitionSource source, IList<ObjectSchemaProperty> properties, string wcfNamespace = null) : base(@namespace, definitionName, source)
        {
            WcfNamespace = wcfNamespace;
            Properties = new ReadOnlyCollection<ObjectSchemaProperty>(properties);
        }
    }
}