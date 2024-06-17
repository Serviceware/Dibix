using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Extensibility;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeAnalysis
{
    public static class SqlCodeAnalysisTask
    {
        public static bool Execute(SqlCodeAnalysisConfiguration configuration, ILockEntryManager lockEntryManager, ILogger logger, TSqlModel model)
        {
            if (logger.HasLoggedErrors)
                return false;

            ExecuteNativeCodeAnalysis(model, logger, configuration);

            ExecuteExtendedCodeAnalysis(configuration, lockEntryManager, logger, model);

            return !logger.HasLoggedErrors;
        }

        private static bool ExecuteNativeCodeAnalysis(TSqlModel model, ILogger logger, SqlCodeAnalysisConfiguration configuration)
        {
            logger.LogMessage("Performing native code analysis...");
            CodeAnalysisServiceSettings settings = new CodeAnalysisServiceSettings
            {
                CodeAnalysisSucceededFile = configuration.StaticCodeAnalysisSucceededFile,
                ResultsFile = configuration.ResultsFile,
                RuleSettings = new CodeAnalysisRuleSettings()
            };
            CodeAnalysisService service = new CodeAnalysisServiceFactory().CreateAnalysisService(model, settings);
            CodeAnalysisResult analysisResult = service.Analyze(model);
            if (analysisResult != null)
            {
                ReportExtensibilityErrors(analysisResult.InitializationErrors, "DatabaseStaticCodeAnalysisRuleLoadingErrorCategory", false, logger);
                ReportExtensibilityErrors(analysisResult.SuppressionErrors, "DatabaseStaticCodeAnalysisMessageSuppressionErrorCategory", false, logger);
                ReportExtensibilityErrors(analysisResult.AnalysisErrors, "DatabaseStaticCodeAnalysisExecutionErrorCategory", false, logger);
                ReportAnalysisResults(analysisResult, logger);
            }
            return analysisResult != null;
        }

        private static void ExecuteExtendedCodeAnalysis(SqlCodeAnalysisConfiguration configuration, ILockEntryManager lockEntryManager, ILogger logger, TSqlModel model)
        {
            logger.LogMessage("Performing extended code analysis...");
            SqlCodeAnalysisRuleEngine codeAnalysisEngine = SqlCodeAnalysisRuleEngine.Create(model, configuration, lockEntryManager, logger);

            foreach (TaskItem inputFile in configuration.Source)
            {
                string inputFilePath = inputFile.GetFullPath();
                AnalyzeItem(inputFilePath, codeAnalysisEngine, logger);
            }

            AnalyzeScripts(null, configuration.ScriptSource.Select(x => x.GetFullPath()), codeAnalysisEngine, logger);
        }

        private static void ReportExtensibilityErrors(ICollection<ExtensibilityError> errors, object category, bool blocksBuild, ILogger logger)
        {
            if (errors == null || errors.Count <= 0)
                return;

            foreach (ExtensibilityError error in errors)
                ReportExtensibilityError(error, category, blocksBuild, logger);
        }

        private static void ReportExtensibilityError(ExtensibilityError error, object category, bool blocksBuild, ILogger logger)
        {
            logger.LogError(error.Message, error.Document, error.Line, error.Column);
        }

        private static void ReportAnalysisResults(CodeAnalysisResult analysisResult, ILogger logger)
        {
            foreach (SqlRuleProblem problem in analysisResult.Problems)
            {
                if (problem.ModelElement.IsExternal())
                    continue;

                string shortRuleId = problem.RuleId.Split('.').Last();
                logger.LogError("StaticCodeAnalysis", shortRuleId, problem.Description, problem.SourceName, problem.StartLine, problem.StartColumn);
            }
        }

        private static void AnalyzeScripts(string parentFile, IEnumerable<string> scriptFiles, ISqlCodeAnalysisRuleEngine codeAnalysisEngine, ILogger logger)
        {
            foreach (string scriptFile in scriptFiles)
            {
                string scriptFilePath = Path.IsPathRooted(scriptFile) ? scriptFile : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(parentFile), scriptFile));
                string directory = Path.GetDirectoryName(scriptFilePath)!;
                string scriptContent = SqlCmdParser.ProcessSqlCmdScript(directory, File.ReadAllText(scriptFilePath));
                AnalyzeItem(scriptFilePath, scriptContent, codeAnalysisEngine, logger);
            }
        }

        private static void AnalyzeItem(string inputFilePath, ISqlCodeAnalysisRuleEngine codeAnalysisEngine, ILogger logger) => AnalyzeItem(inputFilePath, x => x.Analyze(inputFilePath), codeAnalysisEngine, logger);

        private static void AnalyzeItem(string inputFilePath, string scriptContent, ISqlCodeAnalysisRuleEngine codeAnalysisEngine, ILogger logger) => AnalyzeItem(inputFilePath, x => x.AnalyzeScript(inputFilePath, scriptContent), codeAnalysisEngine, logger);

        private static void AnalyzeItem(string inputFilePath, Func<ISqlCodeAnalysisRuleEngine, IEnumerable<SqlCodeAnalysisError>> analyzeFunction, ISqlCodeAnalysisRuleEngine codeAnalysisEngine, ILogger logger)
        {
            try
            {
                foreach (SqlCodeAnalysisError error in analyzeFunction(codeAnalysisEngine))
                {
                    logger.LogError($"DBX{error.RuleId:D3}", error.Message, inputFilePath, error.Line, error.Column);
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