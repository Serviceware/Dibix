using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public sealed class SqlStoredProcedureVisitor : SqlParserVisitor
    {
        public override void ExplicitVisit(CreateProcedureStatement node)
        {
            base.Target.ProcedureName = node.ProcedureReference.Name.BaseIdentifier.Value;
            this.ParseContent(node, node.StatementList);

            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(ProcedureParameter node)
        {
            base.ParseParameter(node);
        }
    }
}