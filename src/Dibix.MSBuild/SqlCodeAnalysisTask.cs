using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Dibix.MSBuild
{
    public sealed class SqlCodeAnalysisTask : SdkTask, ITask
    {
        public string[] Inputs { get; set; }

        protected override IEnumerable<object> CollectParameters()
        {
            yield return this.Inputs;
            yield return base.Log;
        }
    }
}
