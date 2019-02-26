namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParser
    {
        void Read(IExecutionEnvironment environment, SqlParserSourceKind sourceKind, object source, SqlStatementInfo target, ISqlStatementFormatter formatter);
    }
}