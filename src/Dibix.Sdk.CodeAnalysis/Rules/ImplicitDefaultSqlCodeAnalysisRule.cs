using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 25)]
    public sealed class ImplicitDefaultSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        private static readonly TSqlTokenType[] JoinTokenTypes =
        {
            TSqlTokenType.Cross
          , TSqlTokenType.Full
          , TSqlTokenType.Inner
          , TSqlTokenType.Left
          , TSqlTokenType.Outer
          , TSqlTokenType.Right
          , TSqlTokenType.RightOuterJoin
        };

        protected override string ErrorMessageTemplate => "{0}";

        public ImplicitDefaultSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        public override void Visit(CreateIndexStatement node)
        {
            if (!node.Clustered.HasValue)
                base.Fail(node, $"Please specify the clustering (CLUSTERED/NONCLUSTERED) for the index '{node.Name.Value}' and don't rely on the default");
        }

        public override void ExplicitVisit(QualifiedJoin node)
        {
            for (int i = node.FirstTokenIndex; i < node.LastTokenIndex; i++)
            {
                TSqlParserToken joinToken = node.ScriptTokenStream[i];
                if (joinToken.TokenType != TSqlTokenType.Join)
                    continue;

                for (int j = i - 1; j >= node.FirstTokenIndex; j--)
                {
                    TSqlParserToken previousToken = node.ScriptTokenStream[j];
                    if (previousToken.TokenType == TSqlTokenType.WhiteSpace)
                    {
                        continue;
                    }

                    if (!JoinTokenTypes.Contains(previousToken.TokenType))
                    {
                        base.Fail(joinToken, "Please specify the join type explicitly and don't rely on the default");
                    }

                    break;
                }
            }
        }

        protected override void Visit(TableModel tableModel, SchemaObjectName tableName, TableDefinition tableDefinition)
        {
            foreach (ColumnDefinition column in tableDefinition.ColumnDefinitions)
            {
                if (column.IsPersisted || column.Constraints.OfType<NullableConstraintDefinition>().Any())
                    continue;

                string columnName = column.ColumnIdentifier.Value;
                base.Fail(column, $"Please specify a nullable constraint for the column '{tableName.BaseIdentifier.Value}.{columnName}' and don't rely on the default");
            }
        }
    }
}