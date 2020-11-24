using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.CodeAnalysis;
using Microsoft.SqlServer.Dac.Extensibility;
using Microsoft.SqlServer.Dac.Model;

namespace Dibix.Sdk.CodeAnalysis
{
    public static class SqlCodeAnalysisTask
    {
        private static readonly string[] NativeSqlCASuppressions =
        {
            "17e91ede49a40673d7cc2e2cbee3b5af" // [dbo].[hlsysum_queryuserlistagent]#[ag].[fullname]#3372
          , "aa6ad0d8bbb6f5cd66c8d243cb17dccb" // [dbo].[hlsysum_queryuserlistagent]#[ag].[description]#3389
          , "64a7ef12999c6632d6b511d27588139f" // [dbo].[hlsysum_queryorgunitpersonlist]#ag.fullname#3686
          , "6b6cec30fd76d59b6450459d150ee50e" // [dbo].[hlsysum_queryorgunitpersonlist]#[ag].[description]#3699
        };

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
            return Execute
            (
                projectName
              , namingConventionPrefix
              , isEmbedded
              , staticCodeAnalysisSucceededFile
              , resultsFile
              , source
              , scriptSource
              , logger
              , model
            );
        }
        internal static bool Execute
        (
            string projectName
          , string namingConventionPrefix
          , bool isEmbedded
          , string staticCodeAnalysisSucceededFile
          , string resultsFile
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> scriptSource
          , ILogger logger
          , TSqlModel model
        )
        {
            ExecuteNativeCodeAnalysis(model, logger, staticCodeAnalysisSucceededFile, resultsFile);

            SqlCodeAnalysisRuleEngine codeAnalysisEngine = SqlCodeAnalysisRuleEngine.Create(model, projectName, namingConventionPrefix, isEmbedded, logger);

            foreach (TaskItem inputFile in source)
            {
                string inputFilePath = inputFile.GetFullPath();
                AnalyzeItem(inputFilePath, codeAnalysisEngine, logger);
            }

            AnalyzeScripts(null, scriptSource.Select(x => x.GetFullPath()), codeAnalysisEngine, logger);

            codeAnalysisEngine.ResetSuppressions();

            return !logger.HasLoggedErrors;
        }

        private static bool ExecuteNativeCodeAnalysis(TSqlModel model, ILogger logger, string staticCodeAnalysisSucceededFile, string resultsFile)
        {
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

                string hashKey = $"{problem.ModelElement.Name}#{problem.Fragment.Dump()}#{problem.Fragment.StartOffset}";
                string hash = CalculateHash(hashKey);
                if (NativeSqlCASuppressions.Contains(hash))
                    continue;

                string shortRuleId = problem.RuleId.Split('.').Last();
                logger.LogError("StaticCodeAnalysis", shortRuleId, problem.Description, problem.SourceName, problem.StartLine, problem.StartColumn);
            }
        }

        private static string CalculateHash(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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
