namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlStoredProcedureParser : SqlStatementParser<SqlStoredProcedureVisitor>, ISqlStatementParser
    {
        public SqlStoredProcedureParser(bool isEmbedded) : base(isEmbedded) { }
    }
}