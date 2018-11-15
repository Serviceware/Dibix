using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class HeapTableSqlCodeAnalysisRule : SqlCodeAnalysisRule<HeapTableSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 12;
        public override string ErrorMessage => "Table {0} does not have a primary key";
    }

    public sealed class HeapTableSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateTableStatement node)
        {
            if (node.Definition.TableConstraints.OfType<UniqueConstraintDefinition>().All(x => !x.IsPrimaryKey))
                base.Fail(node, node.SchemaObjectName.Identifiers.Last().Value);
        }
    }
}