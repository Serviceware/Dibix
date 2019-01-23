using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class LooseConstraintsSqlCodeAnalysisRule : SqlCodeAnalysisRule<LooseConstraintsSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 26;
        public override string ErrorMessage => "Constraints should be defined within the CREATE TABLE statement{0}";
    }

    public sealed class LooseConstraintsSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(AlterTableAddTableElementStatement node)
        {
            foreach (ConstraintDefinition constraint in node.Definition.TableConstraints)
            {
                base.Fail(constraint, constraint.ConstraintIdentifier != null ? $": {constraint.ConstraintIdentifier.Value}" : null);
            }
        }
    }
}
 