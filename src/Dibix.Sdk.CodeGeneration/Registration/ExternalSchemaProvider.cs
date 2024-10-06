using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ExternalSchemaProvider : ISchemaProvider
    {
        private readonly ICollection<SchemaDefinition> _schemas;

        public ExternalSchemaProvider(string projectDirectory, IEnumerable<TaskItem> references)
        {
            _schemas = Collect(projectDirectory, references).ToArray();
        }

        IEnumerable<SchemaDefinition> ISchemaProvider.Collect() => _schemas;

        private static IEnumerable<SchemaDefinition> Collect(string projectDirectory, IEnumerable<TaskItem> references)
        {
            return references.SelectMany(x => CollectSchemas(Path.GetFullPath(Path.Combine(projectDirectory, x.GetFullPath()))));
        }

        private static IEnumerable<SchemaDefinition> CollectSchemas(string assemblyPath)
        {
            IPersistedCodeGenerationModel model = CodeGenerationModelSerializer.Read(assemblyPath);
            ExternalSchemaOwner owner = new ExternalSchemaOwner(model.DefaultClassName);
            foreach (SchemaDefinition schemaDefinition in model.Schemas)
            {
                schemaDefinition.ExternalSchemaInfo = new ExternalSchemaInfo(owner);
                yield return schemaDefinition;
            }
        }
    }
}