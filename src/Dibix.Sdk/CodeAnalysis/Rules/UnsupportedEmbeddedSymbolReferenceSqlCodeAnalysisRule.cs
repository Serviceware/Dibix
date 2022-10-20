using Dibix.Sdk.Sql;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
    [SqlCodeAnalysisRule(id: 40)]
    public sealed class UnsupportedEmbeddedSymbolReferenceSqlCodeAnalysisRule : SqlCodeAnalysisRule
    {
        protected override string ErrorMessageTemplate => "Unsupported DDL element reference in a DML project";

        public UnsupportedEmbeddedSymbolReferenceSqlCodeAnalysisRule(SqlCodeAnalysisContext context) : base(context) { }

        // Do not use EXEC to a procedure that is defined in the same DML project
        public override void Visit(ExecutableProcedureReference node)
        {
            if (!base.IsEmbedded)
                return;

            if (node.ProcedureReference.ProcedureReference == null)
                return;

            if (!base.Model.TryGetModelElement(node.ProcedureReference.ProcedureReference.Name, out ElementLocation element))
                return;

            if (!this.Model.IsExternal(element))
                base.Fail(node);
        }
    }
}