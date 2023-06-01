using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public class CodeGenerationModel : IPersistedCodeGenerationModel
    {
        public string AreaName { get; set; }
        public string RootNamespace { get; set; }
        public string DefaultClassName { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string OutputDirectory { get; set; }
        public string DefaultOutputName { get; set; }
        public string ClientOutputName { get; set; }
        public CommandTextFormatting CommandTextFormatting { get; set; }
        public string BaseUrl { get; set; }
        public bool EnableExperimentalFeatures { get; set; }
        public IList<ControllerDefinition> Controllers { get; }
        public IList<SecurityScheme> SecuritySchemes { get; }
        public ICollection<SchemaDefinition> Schemas { get; }
        public ICollection<string> AdditionalAssemblyReferences { get; }

        public CodeGenerationModel()
        {
            Controllers = new Collection<ControllerDefinition>();
            SecuritySchemes = new Collection<SecurityScheme>();
            Schemas = new Collection<SchemaDefinition>();
            AdditionalAssemblyReferences = new SortedSet<string>();
        }
    }
}