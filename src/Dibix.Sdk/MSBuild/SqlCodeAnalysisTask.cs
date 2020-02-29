using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Dibix.Sdk.Sql;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Dibix.Sdk.MSBuild
{
    public static class SqlCodeAnalysisTask
    {
        public static bool Execute(string namingConventionPrefix, string databaseSchemaProviderName, string modelCollation, ITaskItem[] source, ITaskItem[] scriptSource, ITaskItem[] sqlReferencePath, ITask task, TaskLoggingHelper logger)
        {
            IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
            ISqlCodeAnalysisRuleEngine codeAnalysisEngine = SqlCodeAnalysisRuleEngine.Create(namingConventionPrefix, databaseSchemaProviderName, modelCollation, source, sqlReferencePath, task, errorReporter);
            if (errorReporter.HasErrors)
                return false;

            foreach (ITaskItem inputFile in source ?? Enumerable.Empty<ITaskItem>())
            {
                string inputFilePath = inputFile.GetFullPath();
                AnalyzeItem(inputFilePath, codeAnalysisEngine, errorReporter);
            }

            AnalyzeScripts(null, (scriptSource ?? Enumerable.Empty<ITaskItem>()).Select(x => x.GetFullPath()), codeAnalysisEngine, errorReporter);

            return !errorReporter.HasErrors;
        }

        private static void AnalyzeScripts(string parentFile, IEnumerable<string> scriptFiles, ISqlCodeAnalysisRuleEngine codeAnalysisEngine, IErrorReporter errorReporter)
        {
            foreach (string scriptFile in scriptFiles)
            {
                string scriptFilePath = Path.IsPathRooted(scriptFile) ? scriptFile : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(parentFile), scriptFile));
                string scriptContent = SqlCmdParser.ProcessSqlCmdScript(File.ReadAllText(scriptFilePath), out ICollection<string> includes);
                AnalyzeItem(scriptFilePath, scriptContent, codeAnalysisEngine, errorReporter);
                AnalyzeScripts(scriptFilePath, includes, codeAnalysisEngine, errorReporter);
            }
        }

        private static void AnalyzeItem(string inputFilePath, ISqlCodeAnalysisRuleEngine codeAnalysisEngine, IErrorReporter errorReporter) => AnalyzeItem(inputFilePath, x => x.Analyze(inputFilePath), codeAnalysisEngine, errorReporter);

        private static void AnalyzeItem(string inputFilePath, string scriptContent, ISqlCodeAnalysisRuleEngine codeAnalysisEngine, IErrorReporter errorReporter) => AnalyzeItem(inputFilePath, x => x.AnalyzeScript(scriptContent), codeAnalysisEngine, errorReporter);

        private static void AnalyzeItem(string inputFilePath, Func<ISqlCodeAnalysisRuleEngine, IEnumerable<SqlCodeAnalysisError>> analyzeFunction, ISqlCodeAnalysisRuleEngine codeAnalysisEngine, IErrorReporter errorReporter)
        {
            try
            {
                foreach (SqlCodeAnalysisError error in analyzeFunction(codeAnalysisEngine))
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
    }
}
