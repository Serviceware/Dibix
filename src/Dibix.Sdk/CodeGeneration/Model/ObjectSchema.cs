using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class ObjectSchema : SchemaDefinition
    {
        public string WcfNamespace { get; set; }
        public IList<ObjectSchemaProperty> Properties { get; }

        public ObjectSchema(string @namespace, string definitionName) : base(@namespace, definitionName)
        {
            this.Properties = new Collection<ObjectSchemaProperty>();
        }
    }
}