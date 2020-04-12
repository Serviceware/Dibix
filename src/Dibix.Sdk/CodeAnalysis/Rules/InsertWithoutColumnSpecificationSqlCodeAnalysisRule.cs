using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 15)]
    public sealed class InsertWithoutColumnSpecificationSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Missing column specification for INSERT statement";

        public override void Visit(InsertSpecification node)
        {
            if (!node.Columns.Any())
                base.Fail(node);
        }

        public override void Visit(InsertMergeAction node)
        {
            if (!node.Columns.Any())
                base.Fail(node);
        }
    }
}