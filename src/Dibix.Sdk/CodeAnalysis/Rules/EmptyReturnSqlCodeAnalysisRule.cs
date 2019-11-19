using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
     public sealed class EmptyReturnSqlCodeAnalysisRule : SqlCodeAnalysisRule<EmptyReturnSqlCodeAnalysisRuleVisitor>
    {
        public override int Id => 3;
        public override string ErrorMessage => "Please specify a return code for the RETURN expression";
    }

    public sealed class EmptyReturnSqlCodeAnalysisRuleVisitor : SqlCodeAnalysisRuleVisitor
    {
        public override void Visit(ProcedureStatementBodyBase node)
        {
            if (node is FunctionStatementBody function && function.ReturnType is TableValuedFunctionReturnType)
                return;

            ReturnSqlCodeAnalysisRuleVisitor visitor = new ReturnSqlCodeAnalysisRuleVisitor(x => base.Fail(x));
            node.Accept(visitor);
        }

        private sealed class ReturnSqlCodeAnalysisRuleVisitor : TSqlFragmentVisitor
        {
            private readonly Action<TSqlFragment> _errorHandler;

            public ReturnSqlCodeAnalysisRuleVisitor(Action<TSqlFragment> errorHandler) => this._errorHandler = errorHandler;

            public override void Visit(ReturnStatement node)
            {
                if (node.Expression == null)
                    this._errorHandler(node);
            }
        }
    }
}