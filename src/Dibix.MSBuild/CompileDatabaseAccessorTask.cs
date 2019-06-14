using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Dibix.MSBuild
{
    public sealed class CompileDatabaseAccessorTask : SdkTask, ITask
    {
        public string ProjectDirectory { get; set; }
        public string Namespace { get; set; }
        public string TargetDirectory { get; set; }
        public string[] Artifacts { get; set; }
        public string[] Contracts { get; set; }
        public bool MultipleAreas { get; set; }
        public string DataLayerName { get; set; }
        public string ContractsLayerName { get; set; }
        public bool IsDML { get; set; }

        [Output]
        public string OutputFilePath { get; set; }

        protected override IEnumerable<object> CollectParameters()
        {
            yield return this.ProjectDirectory;
            yield return this.Namespace;
            yield return this.TargetDirectory;
            yield return this.Artifacts;
            yield return this.Contracts;
            yield return this.MultipleAreas;
            yield return this.DataLayerName;
            yield return this.ContractsLayerName;
            yield return this.IsDML;
            yield return base.Log;
            yield return null;
        }

        protected override void ProcessParameters(object[] args)
        {
            this.OutputFilePath = (string)args[10];
        }
    }
}
