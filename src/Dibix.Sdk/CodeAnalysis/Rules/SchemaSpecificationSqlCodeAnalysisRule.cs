using System;
using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 2)]
    public sealed class SchemaSpecificationSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Missing schema specification";

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