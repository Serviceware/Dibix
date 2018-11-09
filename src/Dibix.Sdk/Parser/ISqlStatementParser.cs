using System.IO;

namespace Dibix.Sdk
{
    public interface ISqlStatementParser
    {
        ISqlStatementFormatter Formatter { get; set; }

        void Read(IExecutionEnvironment environment, Stream source, SqlStatementInfo target);
    }
}