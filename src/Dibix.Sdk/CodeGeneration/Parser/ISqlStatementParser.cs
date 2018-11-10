using System.IO;
using Dibix.Sdk.CodeGeneration.Lint;

namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParser
    {
        SqlLintConfiguration LintConfiguration { get; }
        ISqlStatementFormatter Formatter { get; set; }

        void Read(IExecutionEnvironment environment, Stream source, SqlStatementInfo target);
    }
}