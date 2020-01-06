using System;
using System.Collections.Generic;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class OutputConfiguration
    {
        public Type Writer { get; set; }
        public string RootNamespace { get; set; }
        public string DefaultClassName { get; set; }
        public string ProductName { get; set; }
        public string AreaName { get; set; }
        public CommandTextFormatting Formatting { get; set; }
        public bool GeneratePublicArtifacts { get; set; }
        public bool WriteNamespaces { get; set; }
        public ICollection<string> DetectedReferences { get; }

        public OutputConfiguration()
        {
            this.DetectedReferences = new HashSet<string>();
        }
    }
}