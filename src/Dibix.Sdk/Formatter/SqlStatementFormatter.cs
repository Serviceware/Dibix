using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public abstract class SqlStatementFormatter : ISqlStatementFormatter
    {
        private static readonly Type[] ExcludedTypes = 
        {
            typeof(PredicateSetStatement)
        };

        public abstract string Format(SqlStatementInfo info, StatementList body);

        protected IEnumerable<TSqlStatement> GetStatements(StatementList statementList)
        {
            return statementList.Statements.Where(FilterStatement);
        }

        private static bool FilterStatement(TSqlStatement statement)
        {
            return !ExcludedTypes.Any(statement.GetType().IsAssignableFrom);
        }
    }
}