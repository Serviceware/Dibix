using System;

namespace Dibix.Sdk.CodeGeneration
{
    internal sealed class NoOpParser : ISqlStatementParser
    {
        public ISqlStatementFormatter Formatter { get; set; }

        public void Read(IExecutionEnvironment environment, SqlParserSourceKind sourceKind, object source, SqlStatementInfo target)
        {
            if (sourceKind != SqlParserSourceKind.String)
                throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, "This parser only supports string input");

            target.Content = (string)source;
        }
    }
}