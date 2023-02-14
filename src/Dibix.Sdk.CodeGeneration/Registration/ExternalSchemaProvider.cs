using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class ExternalSchemaProvider : ISchemaProvider
    {
        private readonly ReferencedAssemblyInspector _referencedAssemblyInspector;
        private readonly ICollection<SchemaDefinition> _schemas;

        public ExternalSchemaProvider(ReferencedAssemblyInspector referencedAssemblyInspector)
        {
            this._referencedAssemblyInspector = referencedAssemblyInspector;
            this._schemas = new Collection<SchemaDefinition>();
            this.Collect();
        }

        IEnumerable<SchemaDefinition> ISchemaProvider.Collect() => _schemas;

        private void Collect()
        {
            IEnumerable<SchemaDefinition> schemas = this._referencedAssemblyInspector.Inspect(VisitReferencedAssemblies);
            this._schemas.AddRange(schemas);
        }

        private static ICollection<SchemaDefinition> VisitReferencedAssemblies(IEnumerable<Assembly> assemblies) => assemblies.SelectMany(VisitReferencedAssembly).ToArray();

        private static IEnumerable<SchemaDefinition> VisitReferencedAssembly(Assembly assembly)
        {
            IPersistedCodeGenerationModel model = CodeGenerationModelSerializer.Read(assembly);
            ExternalSchemaOwner owner = new ExternalSchemaOwner(model.DefaultClassName);
            foreach (SchemaDefinition schemaDefinition in model.Schemas)
            {
                schemaDefinition.ExternalSchemaInfo = new ExternalSchemaInfo(owner);
                yield return schemaDefinition;
            }
        }
    }
}