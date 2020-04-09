﻿using System;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    public sealed class SchemaSpecificationSqlCodeAnalysisRule : SqlCodeAnalysisRule<SchemaSpecificationSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 2;
        public override string ErrorMessage => "Missing schema specification";
    }

    public sealed class SchemaSpecificationSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(SchemaObjectName name)
        {
            if (!base.Model.TryGetModelElement(name, out ElementLocation element)) 
                return;

            if (name.SchemaIdentifier != null)
                return;

            if (base.Model.IsDataType(element))
                return;

            // Unfortunately I haven't found a better way yet to check this properly
            // This might allow table names like 'inserted' without schema specification
            if (String.Equals(name.BaseIdentifier.Value, "inserted", StringComparison.OrdinalIgnoreCase)
             || String.Equals(name.BaseIdentifier.Value, "updated", StringComparison.OrdinalIgnoreCase)
             || String.Equals(name.BaseIdentifier.Value, "deleted", StringComparison.OrdinalIgnoreCase))
                return;

            base.Fail(name);
        }
    }
}