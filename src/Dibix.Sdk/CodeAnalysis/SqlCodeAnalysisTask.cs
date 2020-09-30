using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dibix.Sdk.Sql;

namespace Dibix.Sdk.CodeAnalysis
{
    public static class SqlCodeAnalysisTask
    {
        public static bool Execute
        (
            string databaseSchemaProviderName
          , string modelCollation
          , string namingConventionPrefix
          , ICollection<TaskItem> source
          , IEnumerable<TaskItem> scriptSource
          , IEnumerable<TaskItem> sqlReferencePath
          , ILogger logger
        )
        {
            SqlCodeAnalysisRuleEngine codeAnalysisEngine = SqlCodeAnalysisRuleEngine.Create(databaseSchemaProviderName, modelCollation, namingConventionPrefix, source, sqlReferencePath, logger);
            if (logger.HasLoggedErrors)
                return false;

            foreach (TaskItem inputFile in source)
            {
                string inputFilePath = inputFile.GetFullPath();
                AnalyzeItem(inputFilePath, codeAnalysisEngine, logger);
            }

            AnalyzeScripts(null, scriptSource.Select(x => x.GetFullPath()), codeAnalysisEngine, logger);

            codeAnalysisEngine.ResetSuppressions();

            return !logger.HasLoggedErrors;
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
