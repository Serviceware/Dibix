using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dibix.Sdk.Sql;
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
        private readonly HashSet<StringLiteral> _visitedLiterals;

        public LanguageDependentConstantSqlCodeAnalysisRuleVisitor()
        {
            this._visitedLiterals = new HashSet<StringLiteral>();
        }

        public override void Visit(CastCall node) => this.Check(node, node.DataType, node.Parameter);

        public override void Visit(ColumnDefinition node)
        {
            if (node.DefaultConstraint == null)
                return;

            this.Check(node.DefaultConstraint, node.DataType, node.DefaultConstraint.Expression);
        }

        public override void Visit(StringLiteral node)
        {
            if (this._visitedLiterals.Contains(node))
                return;

            if (Regex.IsMatch(node.Value, @"^((\d\d-\d\d-\d\d\d\d)|(\d\d\d\d-\d\d-\d\d)|(\d\d\/\d\d\/\d\d\d\d)|(\d\d\d\d\/\d\d\/\d\d))"))
                base.Fail(node, node.Value);
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

            this._visitedLiterals.Add(stringLiteral);

            base.Fail(expression, target.Dump());
        }
    }
}