namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParser
    {
        ISqlStatementFormatter Formatter { get; set; }

        void Read(IExecutionEnvironment environment, SqlParserSourceKind sourceKind, object source, SqlStatementInfo target);
    }
}