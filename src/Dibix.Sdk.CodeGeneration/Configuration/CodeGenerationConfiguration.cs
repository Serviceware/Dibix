using System.Collections.Generic;
using System.Collections.ObjectModel;
using Dibix.Sdk.Abstractions;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class CodeGenerationConfiguration
    {
        public string ProductName { get; set; }
        public string AreaName { get; set; }
        public bool IsEmbedded { get; set; }
        public bool LimitDdlStatements { get; set; }
        public string ProjectDirectory { get; set; }
        public string OutputDirectory { get; set; }
        public string ExternalAssemblyReferenceDirectory { get; set; }
        public string AccessorTargetName { get; set; }
        public string AccessorTargetFileName { get; set; }
        public string EndpointTargetFileName { get; set; }
        public string ClientTargetFileName { get; set; }
        public string ModelTargetFileName { get; set; }
        public string DocumentationTargetName { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public string BaseUrl { get; set; }
        public string Description { get; set; }
        public bool SupportOpenApiNullableReferenceTypes { get; set; }
        public ConfigurationTemplates ConfigurationTemplates { get; } = new ConfigurationTemplates();
        public ICollection<TaskItem> Source { get; } = new Collection<TaskItem>();
        public ICollection<TaskItem> Contracts { get; } = new Collection<TaskItem>();
        public ICollection<TaskItem> Endpoints { get; } = new Collection<TaskItem>();
        public ICollection<TaskItem> References { get; } = new Collection<TaskItem>();
    }
}