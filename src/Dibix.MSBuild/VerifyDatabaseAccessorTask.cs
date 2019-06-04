using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Dibix.MSBuild
{
    public sealed class VerifyDatabaseAccessorTask : SdkTask, ITask
    {
        public string ProjectDirectory { get; set; }
        public string Namespace { get; set; }
        public string[] AssemblyReferences { get; set; }
        public string[] Inputs { get; set; }

        protected override IEnumerable<object> CollectParameters()
        {
            yield return this.ProjectDirectory;
            yield return this.Namespace;
            yield return this.AssemblyReferences;
            yield return this.Inputs;
            yield return base.Log;
        }
    }
}
