using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlParserVisitor : TSqlFragmentVisitor
    {
        internal string ProductName { get; set; }
        internal string AreaName { get; set; }
        internal TSqlFragmentAnalyzer FragmentAnalyzer { get; set; }
        internal ISqlStatementFormatter Formatter { get; set; }
        internal ITypeResolverFacade TypeResolver { get; set; }
        internal ISchemaRegistry SchemaRegistry { get; set; }
        internal ILogger Logger { get; set; }
        internal SqlStatementInfo Target { get; set; }
        internal ISqlMarkupDeclaration Markup { get; set; }
    }
}