using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
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

            AnalyzeScripts(null, (scriptSource ?? Enumerable.Empty<ITaskItem>()).Select(x => x.GetFullPath()), namingConventionPrefix, codeAnalysisEngine, errorReporter);

            return !errorReporter.HasErrors;
        }

        private static void AnalyzeScripts(string parentFile, IEnumerable<string> scriptFiles, string namingConventionPrefix, ISqlCodeAnalysisRuleEngine codeAnalysisEngine, IErrorReporter errorReporter)
        {
            foreach (string scriptFile in scriptFiles)
            {
                string scriptFilePath = Path.IsPathRooted(scriptFile) ? scriptFile : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(parentFile), scriptFile));
                string scriptContent = GetNormalizedScriptArtifact(scriptFilePath, namingConventionPrefix, out ICollection<string> includes);
                
                if (scriptContent != null)
                    AnalyzeItem(scriptFilePath, scriptContent, codeAnalysisEngine, errorReporter);

                AnalyzeScripts(scriptFilePath, includes, namingConventionPrefix, codeAnalysisEngine, errorReporter);
            }
        }

        private static string GetNormalizedScriptArtifact(string scriptFilePath, string namingConventionPrefix, out ICollection<string> includes)
        {
            string scriptContent = File.ReadAllText(scriptFilePath);
            IList<string> _includes = new Collection<string>();

            // GO is not T-SQL syntax
            string normalizedScript = scriptContent.Replace("GO", ";");

            // Parse SQLCMD syntax
            normalizedScript = Regex.Replace(normalizedScript, @"^:r (?<include>[^\r|\n]+)", x =>
            {
                _includes.Add(x.Groups["include"].Value);
                return null;
            }, RegexOptions.Multiline);
            includes = new Collection<string>(_includes);

            if (String.IsNullOrWhiteSpace(normalizedScript))
                return null;

            // The DACFX model can only compile DDL artifacts
            // This rule does not apply to artifacts with the special build action 'PreDeploy' or 'PostDeploy'
            // To make it work we just make it a DDL statement by wrapping it in an SP
            normalizedScript = $@"CREATE PROCEDURE [{namingConventionPrefix}_scriptwrapper] AS BEGIN
{normalizedScript}
_:
END";

            return normalizedScript;
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
