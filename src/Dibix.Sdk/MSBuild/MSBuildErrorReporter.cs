using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    internal sealed class MSBuildErrorReporter : ErrorReporter, IErrorReporter
    {
        private readonly TaskLoggingHelper _taskLoggingHelper;

        public MSBuildErrorReporter(TaskLoggingHelper taskLoggingHelper) => this._taskLoggingHelper = taskLoggingHelper;

        protected override void ReportErrors() => base.Errors.Each(x => this._taskLoggingHelper.LogError(null, /*error.ErrorNumber*/null, null, x.Source, x.Line, x.Column, 0, 0, x.Text));
    }
}