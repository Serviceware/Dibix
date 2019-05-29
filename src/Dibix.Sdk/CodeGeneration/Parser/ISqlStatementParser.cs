namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParser
    {
        bool Read(SqlParserSourceKind sourceKind, object source, SqlStatementInfo target, ISqlStatementFormatter formatter, IContractResolverFacade contractResolverFacade, IErrorReporter errorReporter);
    }
}