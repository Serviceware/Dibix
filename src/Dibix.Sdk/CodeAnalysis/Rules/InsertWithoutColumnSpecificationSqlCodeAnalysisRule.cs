using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class InsertWithoutColumnSpecificationSqlCodeAnalysisRule : SqlCodeAnalysisRule<InsertWithoutColumnSpecificationSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 15;
        public override string ErrorMessage => "Missing column specification for INSERT statement";
    }

    public sealed class InsertWithoutColumnSpecificationSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(InsertStatement node)
        {
            if (!node.InsertSpecification.Columns.Any())
                base.Fail(node.InsertSpecification);
        }
    }
}