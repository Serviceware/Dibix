using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class CodeGenerationModel : IPersistedCodeGenerationModel
    {
        public string ProductName { get; set; }
        public string AreaName { get; set; }
        public string RootNamespace { get; set; }
        public string DefaultClassName { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string OutputDirectory { get; set; }
        public string DefaultOutputName { get; set; }
        public string ClientOutputName { get; set; }
        public EndpointConfiguration EndpointConfiguration { get; set; }
        public CommandTextFormatting CommandTextFormatting { get; set; }
        public ICollection<SqlStatementDefinition> SqlStatements { get; }
        public ICollection<ContractDefinition> Contracts { get; }
        public IList<ControllerDefinition> Controllers { get; }
        public IList<SecurityScheme> SecuritySchemes { get; }
        public ICollection<SchemaDefinition> Schemas { get; }
        public ICollection<string> AdditionalAssemblyReferences { get; }
        public bool EnableExperimentalFeatures { get; set; }

        public CodeGenerationModel()
        {
            this.SqlStatements = new Collection<SqlStatementDefinition>();
            this.Contracts = new Collection<ContractDefinition>();
            this.Controllers = new Collection<ControllerDefinition>();
            this.SecuritySchemes = new Collection<SecurityScheme>();
            this.Schemas = new Collection<SchemaDefinition>();
            this.AdditionalAssemblyReferences = new SortedSet<string>();
        }
    }
}