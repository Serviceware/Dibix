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
        public IList<UserDefinedTypeSchema> UserDefinedTypes { get; }
        public ICollection<SchemaDefinition> Contracts { get; }
        public IList<ControllerDefinition> Controllers { get; }
        public ICollection<string> AdditionalAssemblyReferences { get; }

        public CodeGenerationModel(CodeGeneratorCompatibilityLevel compatibilityLevel)
        {
            this.CompatibilityLevel = compatibilityLevel;
            this.Statements = new Collection<SqlStatementInfo>();
            this.UserDefinedTypes = new Collection<UserDefinedTypeSchema>();
            this.Contracts = new Collection<SchemaDefinition>();
            this.Controllers = new Collection<ControllerDefinition>();
            this.AdditionalAssemblyReferences = new HashSet<string>();
        }
    }
}
