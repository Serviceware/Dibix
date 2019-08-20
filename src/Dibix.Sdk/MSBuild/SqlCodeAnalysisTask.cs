using System;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    public static class SqlCodeAnalysisTask
    {
        public static bool Execute(string databaseSchemaProviderName, string modelCollation, ITaskItem[] source, ITaskItem[] sqlReferencePath, ITask task, TaskLoggingHelper logger)
        {
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
            ISqlCodeAnalysisRuleEngine codeAnalysis = SqlCodeAnalysisRuleEngine.Create(databaseSchemaProviderName, modelCollation, source, sqlReferencePath, task, errorReporter);
            if (errorReporter.ReportErrors())
                return false;

            foreach (ITaskItem inputFile in source ?? Enumerable.Empty<ITaskItem>())
            {
                string inputFilePath = inputFile.GetMetadata("FullPath");
                try
                {
                    foreach (SqlCodeAnalysisError error in codeAnalysis.Analyze(inputFilePath))
                    {
                        errorReporter.RegisterError(inputFilePath, error.Line, error.Column, error.RuleId.ToString(), $"[Dibix] {error.Message}");
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException($@"Code analysis execution failed at
{inputFilePath}", e);
                }
            }

            return !errorReporter.ReportErrors();
        }
    }
}