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
        public string AccessorTargetFileName { get; set; }
        public string EndpointTargetFileName { get; set; }
        public string PackageMetadataTargetFileName { get; set; }
        public string ClientTargetFileName { get; set; }
        public string ModelTargetFileName { get; set; }
        public string DocumentationTargetName { get; set; }
        public CommandTextFormatting CommandTextFormatting { get; set; }
        public string BaseUrl { get; set; }
        public bool SupportOpenApiNullableReferenceTypes { get; set; }
        public IList<ControllerDefinition> Controllers { get; }
        public IList<SecurityScheme> SecuritySchemes { get; }
        public ICollection<SchemaDefinition> Schemas { get; }

        public CodeGenerationModel()
        {
            Controllers = new Collection<ControllerDefinition>();
            SecuritySchemes = new Collection<SecurityScheme>();
            Schemas = new Collection<SchemaDefinition>();
        }
    }
}