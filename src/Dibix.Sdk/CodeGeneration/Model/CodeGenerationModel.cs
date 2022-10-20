using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class CodeGenerationModel : IPersistedCodeGenerationModel
    {
        public SqlCoreConfiguration GlobalConfiguration { get; }
        public ArtifactGenerationConfiguration ArtifactGenerationConfiguration { get; }
        public string RootNamespace { get; set; }
        public string DefaultClassName { get; set; }
        public CommandTextFormatting CommandTextFormatting { get; set; }
        public ICollection<SqlStatementDefinition> SqlStatements { get; }
        public ICollection<ContractDefinition> Contracts { get; }
        public IList<ControllerDefinition> Controllers { get; }
        public IList<SecurityScheme> SecuritySchemes { get; }
        public ICollection<SchemaDefinition> Schemas { get; }
        public ICollection<string> AdditionalAssemblyReferences { get; }

        public CodeGenerationModel(SqlCoreConfiguration globalConfiguration, ArtifactGenerationConfiguration artifactGenerationConfiguration, string rootNamespace, string defaultClassName)
        {
            GlobalConfiguration = globalConfiguration;
            ArtifactGenerationConfiguration = artifactGenerationConfiguration;
            RootNamespace = rootNamespace;
            DefaultClassName = defaultClassName;
            SqlStatements = new Collection<SqlStatementDefinition>();
            Contracts = new Collection<ContractDefinition>();
            Controllers = new Collection<ControllerDefinition>();
            SecuritySchemes = new Collection<SecurityScheme>();
            Schemas = new Collection<SchemaDefinition>();
            AdditionalAssemblyReferences = new SortedSet<string>();
        }
    }
}