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
            ParameterExpression ruleInstanceVariable = Expression.Variable(ruleType, "instance");
            Expression ruleInstance = Expression.New(ruleType);
            Expression ruleInstanceAssignment = Expression.Assign(ruleInstanceVariable, ruleInstance);
            Expression execution = Expression.Call(ruleInstanceVariable, nameof(SqlLintRule.Execute), null, environmentParameter, fragmentParameter, sourceFilePathParameter);
            Expression hasErrorProperty = Expression.Property(ruleInstanceVariable, nameof(SqlLintRule.HasError));
            Expression result = Expression.Not(hasErrorProperty);
            Expression block = Expression.Block(new[] { ruleInstanceVariable }, ruleInstanceAssignment, execution, result);
            Func<TSqlFragment, string, bool> executionFunction = Expression.Lambda<Func<TSqlFragment, string, bool>>(block, fragmentParameter, sourceFilePathParameter).Compile();
            this.Rules.Add(new SqlLintRuleAccessor(executionFunction));
        }
    }
}