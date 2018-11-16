﻿using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class MultiTableAliasSqlCodeAnalysisRule : SqlCodeAnalysisRule<MultiTableAliasSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 10;
        public override string ErrorMessage => "Unaliased table reference found in multi table reference joins";
    }

    public sealed class MultiTableAliasSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(JoinTableReference join)
        {
            TableReferenceWithAlias unaliasedTable = new[] { join.FirstTableReference, join.SecondTableReference }.OfType<TableReferenceWithAlias>().FirstOrDefault(x => x.Alias == null);
            if (unaliasedTable != null)
                base.Fail(unaliasedTable);
        }
    }
}