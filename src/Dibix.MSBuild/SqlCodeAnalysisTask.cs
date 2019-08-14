using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Dibix.MSBuild
{
    public sealed class SqlCodeAnalysisTask : SdkTask, ITask
    {
        public string DatabaseSchemaProviderName { get; set; }
        public string ModelCollation { get; set; }
        public ITaskItem[] Source { get; set; }
        public ITaskItem[] SqlReferencePath { get; set; }

        protected override IEnumerable<object> CollectParameters()
        {
            yield return this.DatabaseSchemaProviderName;
            yield return this.ModelCollation;
            yield return this.Source;
            yield return this.SqlReferencePath;
            yield return this;
            yield return base.Log;
        }
    }
}
