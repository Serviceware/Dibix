using System.IO;
using Dibix.Sdk.CodeGeneration.Lint;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class NoOpParser : ISqlStatementParser
    {
        public SqlLintConfiguration LintConfiguration { get; private set; }
        public ISqlStatementFormatter Formatter { get; set; }

        public void Read(IExecutionEnvironment environment, Stream source, SqlStatementInfo target)
        {
            using (StreamReader reader = new StreamReader(source))
            {
                target.Content = reader.ReadToEnd();
            }
        }
    }
}