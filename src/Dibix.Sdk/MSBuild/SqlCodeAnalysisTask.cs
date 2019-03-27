using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Dibix.Sdk.CodeAnalysis;
using Dibix.Sdk.CodeGeneration;
using Microsoft.Build.Utilities;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.MSBuild
{
    public static class SqlCodeAnalysisTask
    { 
        public static bool Execute(IEnumerable<string> inputs, TaskLoggingHelper logger)
        {
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                IErrorReporter errorReporter = new MSBuildErrorReporter(logger);
                ISqlCodeAnalysisRuleEngine codeAnalysis = new SqlCodeAnalysisRuleEngine();
                foreach (string inputFilePath in inputs ?? Enumerable.Empty<string>())
                {
                    TSqlParser parser = new TSql140Parser(true);
                    using (Stream stream = File.OpenRead(inputFilePath))
                    {
                        using (TextReader reader = new StreamReader(stream))
                        {
                            TSqlFragment fragment = parser.Parse(reader, out IList<ParseError> _);
                            foreach (SqlCodeAnalysisError error in codeAnalysis.Analyze(null, fragment))
                            {
                                errorReporter.RegisterError(inputFilePath, error.Line, error.Column, error.RuleId.ToString(), $"[Dibix] {error.Message}");
                            }
                        }
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