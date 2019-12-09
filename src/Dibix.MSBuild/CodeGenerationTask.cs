using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;

namespace Dibix.MSBuild
{
    public sealed class CodeGenerationTask : SdkTask, ITask
    {
        [Required]
        public string ProjectDirectory { get; set; }

        [Required]
        public string ProductName { get; set; }
        public string AreaName { get; set; }

        [Required]
        public string DefaultOutputFilePath { get; set; }
        public string ClientOutputFilePath { get; set; }
        public string[] Sources { get; set; }
        public string[] Contracts { get; set; }
        public string[] Endpoints { get; set; }
        public string[] References { get; set; }
        public bool EmbedStatements { get; set; }

        [Output]
        public string[] DetectedReferences { get; set; }

        protected override IEnumerable<object> CollectParameters()
        {
            yield return this.ProjectDirectory;
            yield return this.ProductName;
            yield return this.AreaName;
            yield return this.DefaultOutputFilePath;
            yield return this.ClientOutputFilePath;
            yield return this.Sources;
            yield return this.Contracts;
            yield return this.Endpoints;
            yield return this.References;
            yield return this.EmbedStatements;
            yield return base.Log;
            yield return null;
        }

        protected override void PostExecute(object[] args)
        {
            this.DetectedReferences = (string[])args.Last();
            for (int i = 0; i < this.DetectedReferences.Length; i++)
            {
                string detectedReference = this.DetectedReferences[i];
                string toolPath = Path.Combine(CurrentDirectory, detectedReference);
                if (File.Exists(toolPath))
                    this.DetectedReferences[i] = toolPath;
            }
        }
    }
}
