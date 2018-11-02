using System.IO;

namespace Dibix.Sdk
{
    public interface ISqlStatementParser
    {
        SqlLintConfiguration LintConfiguration { get; }
        ISqlStatementFormatter Formatter { get; set; }

        void Read(IExecutionEnvironment environment, Stream source, SqlStatementInfo target);
    }
}