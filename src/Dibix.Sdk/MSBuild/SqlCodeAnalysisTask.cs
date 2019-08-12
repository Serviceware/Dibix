using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    public static class SqlCodeAnalysisTask
    {
        public static bool Execute(IEnumerable<string> inputs, TaskLoggingHelper logger)
        {
#if DEBUG
            Debugger.Launch();
#endif
            return ExecuteCore(inputs, logger);
        }

        public static bool ExecuteCore(IEnumerable<string> inputs, TaskLoggingHelper logger)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
                ISqlCodeAnalysisRuleEngine codeAnalysis = SqlCodeAnalysisRuleEngine.Create();
                foreach (string inputFilePath in inputs ?? Enumerable.Empty<string>())
                {
                    foreach (SqlCodeAnalysisError error in codeAnalysis.Analyze(inputFilePath))
                    {
                        errorReporter.RegisterError(inputFilePath, error.Line, error.Column, error.RuleId.ToString(), $"[Dibix] {error.Message}");
                    }
                }

                return !errorReporter.ReportErrors();
            }
            finally
            {
                logger.LogMessage($"Executed SQL code analysis in {sw.Elapsed}");
            }
        }
    }
}