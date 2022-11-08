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

        public IEnumerable<SchemaDefinition> Schemas => this._schemas;

        public ExternalSchemaProvider(ReferencedAssemblyInspector referencedAssemblyInspector)
        {
            this._referencedAssemblyInspector = referencedAssemblyInspector;
            this._schemas = new Collection<SchemaDefinition>();
            this.Collect();
        }

        private void Collect()
        {
            IEnumerable<SchemaDefinition> schemas = this._referencedAssemblyInspector.Inspect(VisitReferencedAssemblies);
            this._schemas.AddRange(schemas);
        }

        private static IEnumerable<SchemaDefinition> VisitReferencedAssemblies(IEnumerable<Assembly> assemblies) => assemblies.SelectMany(VisitReferencedAssembly);

        private static IEnumerable<SchemaDefinition> VisitReferencedAssembly(Assembly assembly)
        {
            IPersistedCodeGenerationModel model = CodeGenerationModelSerializer.Read(assembly);
            return model.Schemas.Where(x => SchemaDefinitionSource.Local.HasFlag(x.Source));
        }
    }
}