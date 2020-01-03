namespace Dibix.Sdk.CodeGeneration
{
    public interface ISqlStatementParser
    {
        bool Read(SqlParserSourceKind sourceKind, object source, SqlStatementInfo target, string productName, string areaName, ISqlStatementFormatter formatter, IContractResolverFacade contractResolver, IErrorReporter errorReporter);
    }
}