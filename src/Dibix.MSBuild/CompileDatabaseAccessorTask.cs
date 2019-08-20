using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;

namespace Dibix.MSBuild
{
    public sealed class CompileDatabaseAccessorTask : SdkTask, ITask
    {
        public string ProjectDirectory { get; set; }
        public string Namespace { get; set; }
        public string OutputFilePath { get; set; }
        public string[] Artifacts { get; set; }
        public string[] Contracts { get; set; }
        public string[] Endpoints { get; set; }
        public bool MultipleAreas { get; set; }
        public bool IsDML { get; set; }

        [Output]
        public string[] DetectedReferences { get; set; }

        protected override IEnumerable<object> CollectParameters()
        {
            yield return this.ProjectDirectory;
            yield return this.Namespace;
            yield return this.OutputFilePath;
            yield return this.Artifacts;
            yield return this.Contracts;
            yield return this.Endpoints;
            yield return this.MultipleAreas;
            yield return this.IsDML;
            yield return base.Log;
            yield return null;
        }

        protected override void ProcessParameters(object[] args)
        {
            this.DetectedReferences = (string[])args[9];
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
