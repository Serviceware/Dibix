using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeAnalysis.Rules
{
     [SqlCodeAnalysisRule(id: 3)]
     public sealed class EmptyReturnSqlCodeAnalysisRule : SqlCodeAnalysisRule
     {
        protected override string ErrorMessageTemplate => "Please specify a return code for the RETURN expression";

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