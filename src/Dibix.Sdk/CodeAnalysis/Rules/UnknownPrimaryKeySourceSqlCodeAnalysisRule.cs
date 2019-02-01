﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    /*public sealed class UnknownPrimaryKeySourceSqlCodeAnalysisRule : SqlCodeAnalysisRule<UnknownPrimaryKeySourceSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 29;
        public override string ErrorMessage => "Unknown primary key source: {0}.{1}";
    }

    public sealed class UnknownPrimaryKeySourceSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(CreateTableStatement node)
        {
            if (node.IsTemporaryTable())
                return;

            ICollection<Constraint> constraints = node.Definition.CollectConstraints().ToArray();
            Constraint pk = constraints.SingleOrDefault(x => x.Type == ConstraintType.PrimaryKey);
            if (pk == null || pk.Columns.Count != 1)
                return;

            ColumnDefinition column = node.Definition.ColumnDefinitions.Single(x => x.ColumnIdentifier.Value == pk.Columns[0].Name);
            bool isInteger = ((SqlDataTypeReference)column.DataType).SqlDataTypeOption == SqlDataTypeOption.Int;
            bool isIdentity = column.IdentityOptions != null;
            bool isSurrogateKey = constraints.Any(x => x.Type == ConstraintType.ForeignKey && x.Columns.Any(y => y.Name == column.ColumnIdentifier.Value));

            if (!isInteger || isIdentity || isSurrogateKey)
                return;

            base.Fail(column, node.SchemaObjectName.BaseIdentifier.Value, column.ColumnIdentifier.Value);
        }
    }*/
}