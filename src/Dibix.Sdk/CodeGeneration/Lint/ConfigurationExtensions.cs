using System;
using System.Collections.Generic;
using System.Linq;

namespace Dibix.Sdk.CodeGeneration.Lint
{
    public static class ConfigurationExtensions
    {
        internal static ISqlStatementParserConfigurationExpression EnableDefaultLintRules(this SqlStatementParserConfigurationExpression expression)
        {
            expression.Lint(x =>
            {
                x.IsEnabled = true;
                Type ruleDefinitionType = typeof(SqlLintRule);
                IEnumerable<Type> ruleTypes = ruleDefinitionType.Assembly
                                                                .GetTypes()
                                                                .Where(ruleDefinitionType.IsAssignableFrom)
                                                                .Except(new [] { ruleDefinitionType });

                ruleTypes.Each(x.RegisterRule);
            });
            return expression;
        }
    }
}
