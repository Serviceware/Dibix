using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class CodeGenerationModel
    {
        public string AreaName { get; set; }
        public string RootNamespace { get; set; }
        public string DefaultClassName { get; set; }
        public string BaseUrl { get; set; }
        public CodeGeneratorCompatibilityLevel CompatibilityLevel { get; }
        public CommandTextFormatting CommandTextFormatting { get; set; }
        public IList<SqlStatementDescriptor> Statements { get; }
        public IList<UserDefinedTypeSchema> UserDefinedTypes { get; }
        public ICollection<ContractDefinition> Contracts { get; }
        public IList<ControllerDefinition> Controllers { get; }
        public IList<SecurityScheme> SecuritySchemes { get; }
        public ICollection<SchemaDefinition> Schemas { get; }
        public ICollection<string> AdditionalAssemblyReferences { get; }

        public CodeGenerationModel(CodeGeneratorCompatibilityLevel compatibilityLevel)
        {
            this.CompatibilityLevel = compatibilityLevel;
            this.Statements = new Collection<SqlStatementDescriptor>();
            this.UserDefinedTypes = new Collection<UserDefinedTypeSchema>();
            this.Contracts = new Collection<ContractDefinition>();
            this.Controllers = new Collection<ControllerDefinition>();
            this.SecuritySchemes = new Collection<SecurityScheme>();
            this.Schemas = new Collection<SchemaDefinition>();
            this.AdditionalAssemblyReferences = new SortedSet<string>();
        }
    }
}
