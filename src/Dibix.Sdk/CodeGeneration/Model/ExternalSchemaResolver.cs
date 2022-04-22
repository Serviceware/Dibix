using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Dibix.Sdk.CodeGeneration.Model
{
    internal sealed class ExternalSchemaResolver : IExternalSchemaResolver, ISchemaStore
    {
        private readonly IDictionary<string, ExternalSchemaDefinition> _schemaMap;
        private readonly ISchemaRegistry _schemaRegistry;

        public ICollection<ExternalSchemaDefinition> Schemas => this._schemaMap.Values;

        public ExternalSchemaResolver(ReferencedAssemblyInspector referencedAssemblyInspector, ISchemaRegistry schemaRegistry)
        {
            this._schemaRegistry = schemaRegistry;
            this._schemaMap = Collect(referencedAssemblyInspector).ToDictionary(x => x.SchemaDefinition.FullName);
        }

        public bool TryGetSchema(string fullName, out ExternalSchemaDefinition schema)
        {
            if (!this._schemaMap.TryGetValue(fullName, out schema))
                return false;

            ReferencingSchemaVisitor visitor = new ReferencingSchemaVisitor(this._schemaRegistry, this);
            visitor.Accept(schema.SchemaDefinition);

            return true;
        }

        bool ISchemaStore.TryGetSchema(string fullName, out SchemaDefinition schema)
        {
            if (this._schemaMap.TryGetValue(fullName, out ExternalSchemaDefinition externalSchema))
            {
                schema = externalSchema.SchemaDefinition;
                return true;
            }

            schema = null;
            return false;
        }

        private static IEnumerable<ExternalSchemaDefinition> Collect(ReferencedAssemblyInspector referencedAssemblyInspector)
        {
            ICollection<ExternalSchemaDefinition> schemas = referencedAssemblyInspector.Inspect(VisitReferencedAssemblies);
            return schemas;
        }

        private static ICollection<ExternalSchemaDefinition> VisitReferencedAssemblies(IEnumerable<Assembly> assemblies) => assemblies.SelectMany(VisitReferencedAssembly)
                                                                                                                                      .DistinctBy(x => x.SchemaDefinition.FullName)
                                                                                                                                      .ToArray();

        private static IEnumerable<ExternalSchemaDefinition> VisitReferencedAssembly(Assembly assembly)
        {
            IPersistedCodeGenerationModel model = CodeGenerationModelSerializer.Read(assembly);
            ExternalSchemaOwner owner = new ExternalSchemaOwner(model.DefaultClassName);
            return model.Schemas
                      //.Where(x => SchemaDefinitionSource.Local.HasFlag(x.Source))
                        .Select(x => new ExternalSchemaDefinition(owner, x));
        }

        private sealed class ReferencingSchemaVisitor : SchemaVisitor
        {
            private readonly ISchemaRegistry _schemaRegistry;

            public ReferencingSchemaVisitor(ISchemaRegistry schemaRegistry, ISchemaStore schemaStore) : base(schemaStore)
            {
                this._schemaRegistry = schemaRegistry;
            }

            protected override void Visit(SchemaDefinition schema)
            {
                if (!this._schemaRegistry.IsRegistered(schema.FullName)) 
                    this._schemaRegistry.Populate(schema);
            }
        }
    }
}