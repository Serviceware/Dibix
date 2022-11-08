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
        public string DefaultOutputName { get; set; }
        public string ClientOutputName { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public string BaseUrl { get; set; }
        public string Description { get; set; }
        public bool EnableExperimentalFeatures { get; set; }
        public ICollection<TaskItem> Source { get; } = new Collection<TaskItem>();
        public ICollection<TaskItem> Contracts { get; } = new Collection<TaskItem>();
        public ICollection<TaskItem> Endpoints { get; } = new Collection<TaskItem>();
        public ICollection<TaskItem> References { get; } = new Collection<TaskItem>();
        public ICollection<TaskItem> DefaultSecuritySchemes { get; } = new Collection<TaskItem>();
    }
}