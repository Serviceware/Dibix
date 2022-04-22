using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Extensibility;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeAnalysis
{
    public static class SqlCodeAnalysisTask
    {
        public static bool Execute
        (
            string projectName
          , string databaseSchemaProviderName
          , string modelCollation
          , string namingConventionPrefix
          , bool isEmbedded
          , string staticCodeAnalysisSucceededFile
          , string resultsFile
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> scriptSource
          , ICollection<TaskItem> sqlReferencePath
          , ILogger logger
        )
        {
            TSqlModel model = PublicSqlDataSchemaModelLoader.Load(projectName, databaseSchemaProviderName, modelCollation, source, sqlReferencePath, logger);
            using (LockEntryManager lockEntryManager = LockEntryManager.Create())
            {
                return Execute
                (
                    projectName
                  , new SqlCodeAnalysisConfiguration(namingConventionPrefix)
                  , isEmbedded
                  , staticCodeAnalysisSucceededFile
                  , resultsFile
                  , source
                  , scriptSource
                  , lockEntryManager
                  , logger
                  , model
                );
            }
        }
        internal static bool Execute
        (
            string projectName
          , SqlCodeAnalysisConfiguration configuration
          , bool isEmbedded
          , string staticCodeAnalysisSucceededFile
          , string resultsFile
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> scriptSource
          , LockEntryManager lockEntryManager
          , ILogger logger
          , TSqlModel model
        )
        {
            if (logger.HasLoggedErrors)
                return false;

            ExecuteNativeCodeAnalysis(model, logger, staticCodeAnalysisSucceededFile, resultsFile);

            ExecuteExtendedCodeAnalysis(projectName, configuration, isEmbedded, source, scriptSource, lockEntryManager, logger, model);

            return !logger.HasLoggedErrors;
        }

        private static bool ExecuteNativeCodeAnalysis(TSqlModel model, ILogger logger, string staticCodeAnalysisSucceededFile, string resultsFile)
        {
            logger.LogMessage("Performing native code analysis...");
            CodeAnalysisServiceSettings settings = new CodeAnalysisServiceSettings
            {
                CodeAnalysisSucceededFile = staticCodeAnalysisSucceededFile,
                ResultsFile = resultsFile,
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

        private static void ExecuteExtendedCodeAnalysis(string projectName, SqlCodeAnalysisConfiguration configuration, bool isEmbedded, ICollection<TaskItem> source, IEnumerable<TaskItem> scriptSource, LockEntryManager lockEntryManager, ILogger logger, TSqlModel model)
        {
            logger.LogMessage("Performing extended code analysis...");
            SqlCodeAnalysisRuleEngine codeAnalysisEngine = SqlCodeAnalysisRuleEngine.Create(model, projectName, configuration, isEmbedded, lockEntryManager, logger);

            foreach (TaskItem inputFile in source)
            {
                string inputFilePath = inputFile.GetFullPath();
                AnalyzeItem(inputFilePath, codeAnalysisEngine, logger);
            }

            AnalyzeScripts(null, scriptSource.Select(x => x.GetFullPath()), codeAnalysisEngine, logger);
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
            logger.LogError(error.ErrorCode.ToString(), error.Message, error.Document, error.Line, error.Column);
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
                string scriptContent = SqlCmdParser.ProcessSqlCmdScript(File.ReadAllText(scriptFilePath), out ICollection<string> includes);

                if (scriptContent != null)
                    AnalyzeItem(scriptFilePath, scriptContent, codeAnalysisEngine, logger);

                AnalyzeScripts(scriptFilePath, includes, codeAnalysisEngine, logger);
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
                    logger.LogError($"DBX{error.RuleId:000}", error.Message, inputFilePath, error.Line, error.Column);
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