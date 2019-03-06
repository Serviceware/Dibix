namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParser
    {
        void Read(SqlParserSourceKind sourceKind, object source, SqlStatementInfo target, ISqlStatementFormatter formatter, ITypeLoaderFacade typeLoaderFacade, IErrorReporter errorReporter);
    }
}