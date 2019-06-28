using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class OutputConfiguration
    {
        public Type Writer { get; set; }
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public CommandTextFormatting Formatting { get; set; }
        public bool GeneratePublicArtifacts { get; set; }
        public ICollection<string> DetectedReferences { get; }

        public OutputConfiguration()
        {
            this.DetectedReferences = new Collection<string>();
        }
    }
}