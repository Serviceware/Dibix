using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    internal sealed class SqlLintRuleAccessor
    {
        private readonly Action<TSqlFragment, string> _executionFunction;

        public SqlLintRuleAccessor(Action<TSqlFragment, string> executionFunction)
        {
            this._executionFunction = executionFunction;
        }

        public void Execute(TSqlFragment fragment, string sourceFilePath)
        {
            this._executionFunction(fragment, sourceFilePath);
        }
    }
}