using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 26)]
    public sealed class LooseConstraintsSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Constraints should be defined within the CREATE TABLE statement{0}";

        public LooseConstraintsSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(AlterTableAddTableElementStatement node)
        {
            foreach (ConstraintDefinition constraint in node.Definition.TableConstraints)
            {
                base.Fail(constraint, constraint.ConstraintIdentifier != null ? $": {constraint.ConstraintIdentifier.Value}" : null);
            }
        }
    }
}
 