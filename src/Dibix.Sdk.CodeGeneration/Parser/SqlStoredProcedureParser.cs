namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlStoredProcedureParser : SqlStatementParser<SqlStoredProcedureVisitor>, ISqlStatementParser
    {
        public SqlStoredProcedureParser() : this(false) { }
        public SqlStoredProcedureParser(bool requireExplicitMarkup) : base(requireExplicitMarkup) { }
    }
}