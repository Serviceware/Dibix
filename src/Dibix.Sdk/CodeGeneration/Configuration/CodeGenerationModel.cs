using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class CodeGenerationModel
    {
        public string AreaName { get; set; }
        public string RootNamespace { get; set; }
        public string DefaultClassName { get; set; }
        public CodeGeneratorCompatibilityLevel CompatibilityLevel { get; }
        public CommandTextFormatting CommandTextFormatting { get; set; }
        public IList<SqlStatementInfo> Statements { get; }
        public IList<UserDefinedTypeDefinition> UserDefinedTypes { get; }
        public ICollection<ContractDefinition> Contracts { get; }
        public IList<ControllerDefinition> Controllers { get; }
        public ICollection<string> AdditionalAssemblyReferences { get; }

        public CodeGenerationModel(CodeGeneratorCompatibilityLevel compatibilityLevel)
        {
            this.CompatibilityLevel = compatibilityLevel;
            this.Statements = new Collection<SqlStatementInfo>();
            this.UserDefinedTypes = new Collection<UserDefinedTypeDefinition>();
            this.Contracts = new Collection<ContractDefinition>();
            this.Controllers = new Collection<ControllerDefinition>();
            this.AdditionalAssemblyReferences = new HashSet<string>();
        }
    }
}
