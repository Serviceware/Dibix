using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Dibix.Sdk
{
    public class SqlLintConfiguration
    {
        public bool IsEnabled { get; set; }
        internal IExecutionEnvironment Environment { get; set; }
        internal ICollection<SqlLintRuleAccessor> Rules { get; }

        public SqlLintConfiguration()
        {
            this.Rules = new Collection<SqlLintRuleAccessor>();
        }

        internal void RegisterRule(Type ruleType)
        {
            Expression environmentParameter = Expression.Constant(this.Environment);
            ParameterExpression fragmentParameter = Expression.Parameter(typeof(TSqlFragment), "fragment");
            ParameterExpression sourceFilePathParameter = Expression.Parameter(typeof(string), "sourceFilePath");
            Expression ruleInstance = Expression.New(ruleType);
            Expression ruleInstanceCast = Expression.Convert(ruleInstance, typeof(SqlLintRule));
            Expression execution = Expression.Call(ruleInstanceCast, "Execute", null, environmentParameter, fragmentParameter, sourceFilePathParameter);
            Action<TSqlFragment, string> executionFunction = Expression.Lambda<Action<TSqlFragment, string>>(execution, fragmentParameter, sourceFilePathParameter).Compile();
            this.Rules.Add(new SqlLintRuleAccessor(executionFunction));
        }
    }
}