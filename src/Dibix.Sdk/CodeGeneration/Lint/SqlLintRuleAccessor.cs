using System;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk.CodeGeneration.Lint
{
    internal sealed class SqlLintRuleAccessor
    {
        private readonly Func<TSqlFragment, string, bool> _executionFunction;

        public SqlLintRuleAccessor(Func<TSqlFragment, string, bool> executionFunction)
        {
            this._executionFunction = executionFunction;
        }

        public bool Execute(TSqlFragment fragment, string sourceFilePath)
        {
            return this._executionFunction(fragment, sourceFilePath);
        }
    }
}