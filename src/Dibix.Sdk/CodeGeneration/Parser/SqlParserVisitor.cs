using Dibix.Sdk.Abstractions;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public abstract class SqlParserVisitor : TSqlFragmentVisitor
    {
        internal string Source { get; set; }
        internal string DefinitionName { get; set; }
        internal ArtifactGenerationConfiguration Configuration { get; set; }
        internal SqlStatementDefinition Definition { get; set; }
        internal TSqlFragmentAnalyzer FragmentAnalyzer { get; set; }
        internal ISqlStatementFormatter Formatter { get; set; }
        internal ITypeResolverFacade TypeResolver { get; set; }
        internal ISchemaRegistry SchemaRegistry { get; set; }
        internal ISchemaDefinitionResolver SchemaDefinitionResolver { get; set; }
        internal ILogger Logger { get; set; }
        internal ISqlMarkupDeclaration Markup { get; set; }
    }
}