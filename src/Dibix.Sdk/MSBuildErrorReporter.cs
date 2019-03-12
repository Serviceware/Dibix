using System.CodeDom.Compiler;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk
{
    internal sealed class MSBuildErrorReporter : ErrorReporter, IErrorReporter
    {
        private readonly TaskLoggingHelper _taskLoggingHelper;

        public MSBuildErrorReporter(TaskLoggingHelper taskLoggingHelper)
        {
            this._taskLoggingHelper = taskLoggingHelper;
        }

        public override bool ReportErrors()
        {
            foreach (CompilerError error in base.Errors)
            {
                if (!error.IsWarning)
                    this._taskLoggingHelper.LogError(null, error.ErrorNumber, null, error.FileName, error.Line, error.Column, 0, 0, error.ErrorText);
                else
                    this._taskLoggingHelper.LogWarning(null, error.ErrorNumber, null, error.FileName, error.Line, error.Column, 0, 0, error.ErrorText);
            }

            return base.HasErrors;
        }
    }
}