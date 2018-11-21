using System.Collections.Generic;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class LanguageDependentConstantSqlCodeAnalysisRule : SqlCodeAnalysisRule<LanguageDependentConstantSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 20;
        public override string ErrorMessage => "Found language dependent expression: {0}";
    }

    public sealed class LanguageDependentConstantSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        private static readonly HashSet<SqlDataTypeOption> LanguageDependentDataTypes = new HashSet<SqlDataTypeOption>
        {
            SqlDataTypeOption.Date
          , SqlDataTypeOption.DateTime
          , SqlDataTypeOption.DateTime2
          , SqlDataTypeOption.SmallDateTime
          , SqlDataTypeOption.Time
          , SqlDataTypeOption.Timestamp
        };

        public override void Visit(CastCall node) => this.Check(node, node.DataType, node.Parameter);

        public override void Visit(ColumnDefinition node)
        {
            if (node.DefaultConstraint == null)
                return;

            this.Check(node.DefaultConstraint, node.DataType, node.DefaultConstraint.Expression);
        }

        private void Check(TSqlFragment target, DataTypeReference dataType, ScalarExpression expression)
        {
            if (!(dataType is SqlDataTypeReference sqlDataType) || !LanguageDependentDataTypes.Contains(sqlDataType.SqlDataTypeOption))
                return;

            StringLiteral stringLiteral = expression as StringLiteral;
            ParenthesisExpression parenthesisExpression = expression as ParenthesisExpression;
            while (parenthesisExpression != null)
            {
                stringLiteral = parenthesisExpression.Expression as StringLiteral;
                parenthesisExpression = parenthesisExpression.Expression as ParenthesisExpression;
            }

            if (stringLiteral == null)
                return;

            base.Fail(expression, target.Dump());
        }
    }
}