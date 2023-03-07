using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlParserVisitor : TSqlFragmentVisitor
    {
        internal string Source { private protected get; set; }
        internal string DefinitionName { private protected get; set; }
        internal string ProductName { private protected get; set; }
        internal string AreaName { private protected get; set; }
        internal SqlStatementDefinition Definition { get; private set; }
        internal TSqlFragmentAnalyzer FragmentAnalyzer { private protected get; set; }
        internal ISqlStatementFormatter Formatter { private protected get; set; }
        internal ITypeResolverFacade TypeResolver { private protected get; set; }
        internal ISchemaRegistry SchemaRegistry { private protected get; set; }
        internal ILogger Logger { private protected get; set; }
        internal bool IsEmbedded { private protected get; set; }

        private protected void SetDefinition(SqlStatementDefinition definition)
        {
            if (Definition != null)
                Logger.LogError("Only one definition per file is supported", definition.Location);

            Definition = definition;
        }
    }
}