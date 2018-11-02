using System.IO;

namespace Dibix.Sdk
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