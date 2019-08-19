using System;
using System.Collections.Generic;
using System.Linq;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.Dac.Model;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class SuspiciousJoinPredicateSqlCodeAnalysisRule : SqlCodeAnalysisRule<SuspiciousJoinPredicateSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 34;
        public override string ErrorMessage => "{0}";
    }

    public sealed class SuspiciousJoinPredicateSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(QualifiedJoin node)
        {
            if (!(node.FirstTableReference is NamedTableReference leftTable)
             || !(node.SecondTableReference is NamedTableReference rightTable))
                return;

            BooleanComparisonVisitor visitor = new BooleanComparisonVisitor(base.Model, leftTable, rightTable, x => base.Fail(x, $"Different data types in join predicate may lead to scan at runtime: {x.Dump()}"));
            node.SearchCondition.Accept(visitor);
        }

        private sealed class BooleanComparisonVisitor : TSqlFragmentVisitor
        {
            private readonly SqlModel _model;
            private readonly IDictionary<string, SchemaObjectName> _tables;
            private readonly Action<TSqlFragment> _errorHandler;

            public BooleanComparisonVisitor(SqlModel model, NamedTableReference leftTable, NamedTableReference rightTable, Action<TSqlFragment> errorHandler)
            {
                this._model = model;
                this._tables = new[] { leftTable, rightTable }.ToDictionary(x => x.Alias.Value, x => x.SchemaObject);
                this._errorHandler = errorHandler;
            }

            public override void Visit(BooleanComparisonExpression node)
            {
                ColumnReferenceVisitor leftVisitor = new ColumnReferenceVisitor();
                ColumnReferenceVisitor rightVisitor = new ColumnReferenceVisitor();
                node.FirstExpression.Accept(leftVisitor);
                node.SecondExpression.Accept(rightVisitor);

                if (leftVisitor.ColumnReference == null || rightVisitor.ColumnReference == null)
                    return;

                SchemaObjectName leftTableName = this._tables[leftVisitor.ColumnReference.MultiPartIdentifier[0].Value];
                string leftColumnName = leftVisitor.ColumnReference.GetName().Value;
                SqlDataType leftColumnType = SqlDataType.Unknown;
                if (!this._model.TryGetColumnType(leftTableName, leftColumnName, ref leftColumnType))
                    return;

                SchemaObjectName rightTableName = this._tables[rightVisitor.ColumnReference.MultiPartIdentifier[0].Value];
                string rightColumnName = rightVisitor.ColumnReference.GetName().Value;
                SqlDataType rightColumnType = SqlDataType.Unknown;
                if (!this._model.TryGetColumnType(rightTableName, rightColumnName, ref rightColumnType))
                    return;

                if (leftColumnType != rightColumnType)
                    this._errorHandler(node);
            }
        }

        private sealed class ColumnReferenceVisitor : TSqlFragmentVisitor
        {
            public ColumnReferenceExpression ColumnReference { get; private set; }

            public override void Visit(ColumnReferenceExpression node)
            {
                this.ColumnReference = node;
            }
        }
    }
}
 