using System;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration
{
    public sealed class SqlStoredProcedureVisitor : SqlParserVisitor
    {
        public override void ExplicitVisit(CreateProcedureStatement node)
        {
            base.Target.ProcedureName = String.Join(".", node.ProcedureReference.Name.Identifiers.Select(x => Identifier.EncodeIdentifier(x.Value)));
            
            foreach (ProcedureParameter parameter in node.Parameters) 
                base.ParseParameter(parameter);

            this.ParseContent(base.Target, node, node.StatementList);
            base.ExplicitVisit(node);
        }
    }
}